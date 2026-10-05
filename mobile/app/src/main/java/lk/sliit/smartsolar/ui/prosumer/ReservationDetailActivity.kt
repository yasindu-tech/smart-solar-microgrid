/**
 * File        : ReservationDetailActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.prosumer
 * Description : Reservation detail screen displaying reservation status, generated
 *               transaction QR code (for Approved status), node details, energy transfer,
 *               and status-dependent actions (Modify/Cancel).
 */

package lk.sliit.smartsolar.ui.prosumer

import android.content.ClipData
import android.content.ClipboardManager
import android.content.Context
import android.content.Intent
import android.graphics.Bitmap
import android.graphics.Color
import android.os.Bundle
import android.view.View
import android.widget.TextView
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import com.google.zxing.BarcodeFormat
import com.google.zxing.qrcode.QRCodeWriter
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.local.NodeCacheDao
import lk.sliit.smartsolar.data.local.SmartSolarDatabase
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.data.remote.dto.ReservationResponse
import lk.sliit.smartsolar.databinding.ActivityReservationDetailBinding
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

class ReservationDetailActivity : AppCompatActivity() {

    private lateinit var binding: ActivityReservationDetailBinding
    private lateinit var nodeCacheDao: NodeCacheDao

    private var reservationId: String = ""
    private var passedNodeName: String? = null
    private var currentReservation: ReservationResponse? = null
    private var activeQrToken: String? = null

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityReservationDetailBinding.inflate(layoutInflater)
        setContentView(binding.root)

        nodeCacheDao = NodeCacheDao(SmartSolarDatabase.getInstance(this))

        reservationId = intent.getStringExtra(EXTRA_RESERVATION_ID).orEmpty()
        passedNodeName = intent.getStringExtra(EXTRA_NODE_NAME)

        setupToolbar()
        setupListeners()

        if (reservationId.isNotBlank()) {
            loadReservationDetails()
        } else {
            Toast.makeText(this, "Invalid Reservation ID", Toast.LENGTH_SHORT).show()
            finish()
        }
    }

    private fun setupToolbar() {
        setSupportActionBar(binding.toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        binding.toolbar.setNavigationOnClickListener { finish() }
    }

    private fun setupListeners() {
        binding.btnCopyCode.setOnClickListener {
            val token = activeQrToken ?: currentReservation?.qrToken ?: currentReservation?.reservationId
            if (!token.isNullOrBlank()) {
                val clipboard = getSystemService(CLIPBOARD_SERVICE) as ClipboardManager
                val clip = ClipData.newPlainText("QR Token", token)
                clipboard.setPrimaryClip(clip)
                Toast.makeText(this, getString(R.string.qr_code_copied_toast), Toast.LENGTH_SHORT).show()
            }
        }

        binding.btnModify.setOnClickListener {
            val res = currentReservation ?: return@setOnClickListener
            val intent = Intent(this, CreateBookingActivity::class.java).apply {
                putExtra(CreateBookingActivity.EXTRA_NODE_ID, res.nodeId)
            }
            startActivity(intent)
        }

        binding.btnCancel.setOnClickListener {
            confirmCancelReservation()
        }
    }

    private fun loadReservationDetails() {
        binding.progressBar.visibility = View.VISIBLE

        lifecycleScope.launch {
            var fetchedReservation: ReservationResponse? = null
            var resolvedNodeName: String? = passedNodeName

            try {
                // 1. Fetch single reservation by ID
                val response = withContext(Dispatchers.IO) {
                    ApiClient.reservationsApi.getReservationById(reservationId)
                }

                if (response.isSuccessful && response.body() != null) {
                    fetchedReservation = response.body()
                }
            } catch (_: Exception) {
                // Ignore network failure, fall back if needed
            }

            // 2. Resolve Node Name if missing
            val targetNodeId = fetchedReservation?.nodeId
            if (resolvedNodeName.isNullOrBlank() && !targetNodeId.isNullOrBlank()) {
                try {
                    val nodesResp = withContext(Dispatchers.IO) {
                        ApiClient.nodesApi.getNodes(active = null, page = 1, pageSize = 100)
                    }
                    if (nodesResp.isSuccessful && nodesResp.body() != null) {
                        val node = nodesResp.body()!!.items.firstOrNull { it.nodeId == targetNodeId }
                        resolvedNodeName = node?.name
                    }
                } catch (_: Exception) {
                    val cachedList = withContext(Dispatchers.IO) { nodeCacheDao.getAll() }
                    resolvedNodeName = cachedList.firstOrNull { it.nodeId == targetNodeId }?.name
                }
            }

            binding.progressBar.visibility = View.GONE

            if (fetchedReservation != null) {
                currentReservation = fetchedReservation
                bindReservation(fetchedReservation, resolvedNodeName)
            } else {
                Toast.makeText(this@ReservationDetailActivity, getString(R.string.bookings_load_failed), Toast.LENGTH_SHORT).show()
            }
        }
    }

    private fun bindReservation(reservation: ReservationResponse, nodeName: String?) {
        binding.tvReservationId.text = reservation.reservationId
        applyStatusBadge(binding.tvStatus, reservation.status)

        val nodeDisplayName = nodeName
            ?: getString(R.string.dashboard_node_label, reservation.nodeId)

        binding.tvDetailNode.text = getString(R.string.reservation_detail_node, nodeDisplayName)
        binding.tvDetailType.text = getString(
            R.string.reservation_detail_type,
            getString(R.string.reservation_detail_type_default)
        )
        binding.tvDetailStarts.text = getString(
            R.string.reservation_detail_starts,
            formatDateTime(reservation.slotStartTime)
        )
        binding.tvDetailEnds.text = getString(
            R.string.reservation_detail_ends,
            formatDateTime(reservation.slotEndTime)
        )
        binding.tvDetailEnergy.text = getString(
            R.string.reservation_detail_energy,
            reservation.requestedKwh
        )

        val status = reservation.status

        // QR Code visibility and generation (ONLY for Approved status)
        if (status.equals(STATUS_APPROVED, ignoreCase = true)) {
            binding.cardQrCode.visibility = View.VISIBLE
            val qrContent = reservation.qrToken ?: reservation.reservationId
            activeQrToken = qrContent
            val qrBitmap = generateQrBitmap(qrContent, 512)
            if (qrBitmap != null) {
                binding.ivQrCode.setImageBitmap(qrBitmap)
            }
        } else {
            binding.cardQrCode.visibility = View.GONE
            activeQrToken = null
        }

        // Status-specific fields & action buttons
        when {
            status.equals(STATUS_APPROVED, ignoreCase = true) -> {
                binding.btnModify.visibility = View.VISIBLE
                binding.btnCancel.visibility = View.GONE

                val operator = if (!reservation.completedByOperatorId.isNullOrBlank()) {
                    reservation.completedByOperatorId
                } else {
                    getString(R.string.reservation_detail_default_approved_by)
                }
                binding.tvDetailApprovedBy.text = getString(R.string.reservation_detail_approved_by, operator)
                binding.tvDetailApprovedBy.visibility = View.VISIBLE

                binding.tvDetailTransferred.visibility = View.GONE
                binding.tvDetailCompleted.visibility = View.GONE
                binding.tvDetailCancelReason.visibility = View.GONE
            }

            status.equals(STATUS_PENDING, ignoreCase = true) -> {
                binding.btnModify.visibility = View.VISIBLE
                binding.btnCancel.visibility = View.VISIBLE

                binding.tvDetailApprovedBy.visibility = View.GONE
                binding.tvDetailTransferred.visibility = View.GONE
                binding.tvDetailCompleted.visibility = View.GONE
                binding.tvDetailCancelReason.visibility = View.GONE
            }

            status.equals(STATUS_COMPLETED, ignoreCase = true) -> {
                binding.btnModify.visibility = View.GONE
                binding.btnCancel.visibility = View.GONE

                val delivered = reservation.deliveredKwh ?: reservation.requestedKwh
                binding.tvDetailTransferred.text = getString(R.string.reservation_detail_transferred, delivered)
                binding.tvDetailTransferred.visibility = View.VISIBLE

                val operator = if (!reservation.completedByOperatorId.isNullOrBlank()) {
                    reservation.completedByOperatorId
                } else {
                    getString(R.string.reservation_detail_default_approved_by)
                }
                binding.tvDetailApprovedBy.text = getString(R.string.reservation_detail_approved_by, operator)
                binding.tvDetailApprovedBy.visibility = View.VISIBLE

                val completedTime = reservation.completedAt ?: reservation.updatedAt ?: reservation.slotEndTime
                binding.tvDetailCompleted.text = getString(
                    R.string.reservation_detail_completed,
                    formatDateTime(completedTime)
                )
                binding.tvDetailCompleted.visibility = View.VISIBLE

                binding.tvDetailCancelReason.visibility = View.GONE
            }

            status.equals(STATUS_CANCELLED, ignoreCase = true) -> {
                binding.btnModify.visibility = View.GONE
                binding.btnCancel.visibility = View.GONE

                val reason = if (!reservation.cancelledBy.isNullOrBlank()) {
                    reservation.cancelledBy
                } else {
                    getString(R.string.reservation_detail_default_cancel_reason)
                }
                binding.tvDetailCancelReason.text = getString(R.string.reservation_detail_cancel_reason, reason)
                binding.tvDetailCancelReason.visibility = View.VISIBLE

                binding.tvDetailTransferred.visibility = View.GONE
                binding.tvDetailApprovedBy.visibility = View.GONE
                binding.tvDetailCompleted.visibility = View.GONE
            }

            else -> {
                binding.btnModify.visibility = View.GONE
                binding.btnCancel.visibility = View.GONE

                binding.tvDetailTransferred.visibility = View.GONE
                binding.tvDetailApprovedBy.visibility = View.GONE
                binding.tvDetailCompleted.visibility = View.GONE
                binding.tvDetailCancelReason.visibility = View.GONE
            }
        }
    }

    private fun confirmCancelReservation() {
        AlertDialog.Builder(this)
            .setTitle(R.string.cancel_dialog_title)
            .setMessage(R.string.cancel_dialog_message)
            .setPositiveButton(R.string.cancel_dialog_confirm) { _, _ ->
                performCancelReservation()
            }
            .setNegativeButton(R.string.cancel_btn, null)
            .show()
    }

    private fun performCancelReservation() {
        binding.progressBar.visibility = View.VISIBLE
        lifecycleScope.launch {
            try {
                val response = withContext(Dispatchers.IO) {
                    ApiClient.reservationsApi.cancelReservation(reservationId)
                }

                if (response.isSuccessful && response.body() != null) {
                    Toast.makeText(this@ReservationDetailActivity, getString(R.string.cancel_success_toast), Toast.LENGTH_SHORT).show()
                    currentReservation = response.body()
                    bindReservation(response.body()!!, passedNodeName)
                } else {
                    Toast.makeText(this@ReservationDetailActivity, getString(R.string.cancel_failed_toast), Toast.LENGTH_SHORT).show()
                }
            } catch (_: Exception) {
                Toast.makeText(this@ReservationDetailActivity, getString(R.string.login_error_network), Toast.LENGTH_SHORT).show()
            } finally {
                binding.progressBar.visibility = View.GONE
            }
        }
    }

    private fun applyStatusBadge(textView: TextView, status: String?) {
        textView.text = status.orEmpty()
        val context = textView.context

        val (bgRes, textRes) = when {
            status.equals(STATUS_APPROVED, ignoreCase = true) ->
                R.color.sg_primary to R.color.sg_white
            status.equals(STATUS_PENDING, ignoreCase = true) ->
                R.color.sg_accent to R.color.sg_text
            status.equals(STATUS_COMPLETED, ignoreCase = true) ->
                R.color.sg_success to R.color.sg_white
            status.equals(STATUS_CANCELLED, ignoreCase = true) ->
                R.color.sg_text_muted to R.color.sg_white
            else ->
                R.color.sg_surface_muted to R.color.sg_text_secondary
        }

        textView.backgroundTintList = ContextCompat.getColorStateList(context, bgRes)
        textView.setTextColor(ContextCompat.getColor(context, textRes))
    }

    private fun generateQrBitmap(contents: String, sizePx: Int = 512): Bitmap? {
        if (contents.isBlank()) return null
        return try {
            val writer = QRCodeWriter()
            val bitMatrix = writer.encode(contents, BarcodeFormat.QR_CODE, sizePx, sizePx)
            val width = bitMatrix.width
            val height = bitMatrix.height
            val bmp = Bitmap.createBitmap(width, height, Bitmap.Config.RGB_565)
            for (x in 0 until width) {
                for (y in 0 until height) {
                    bmp.setPixel(x, y, if (bitMatrix[x, y]) Color.BLACK else Color.WHITE)
                }
            }
            bmp
        } catch (_: Exception) {
            null
        }
    }

    private fun formatDateTime(value: String?): String {
        if (value.isNullOrBlank()) return ""
        return try {
            val ldt = try {
                OffsetDateTime.parse(value).toLocalDateTime()
            } catch (_: Exception) {
                LocalDateTime.parse(value)
            }
            ldt.format(DATE_TIME_FORMATTER)
        } catch (_: Exception) {
            value
        }
    }

    companion object {
        const val EXTRA_RESERVATION_ID = "EXTRA_RESERVATION_ID"
        const val EXTRA_NODE_NAME = "EXTRA_NODE_NAME"

        private const val STATUS_APPROVED = "Approved"
        private const val STATUS_PENDING = "Pending"
        private const val STATUS_COMPLETED = "Completed"
        private const val STATUS_CANCELLED = "Cancelled"

        private val DATE_TIME_FORMATTER = DateTimeFormatter.ofPattern("EEE, dd MMM yyyy  HH:mm", Locale.US)
    }
}
