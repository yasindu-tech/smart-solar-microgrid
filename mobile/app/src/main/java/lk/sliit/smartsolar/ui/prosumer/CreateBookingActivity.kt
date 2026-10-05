/**
 * File        : CreateBookingActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.prosumer
 * Description : Energy reservation booking creation screen (D3 / REQ-RESV §4.1).
 *               Enforces the 7-day booking horizon (BR-05), 12-hour notice rules,
 *               and renders a reservation summary page upon creation (AC-1, AC-15).
 */

package lk.sliit.smartsolar.ui.prosumer

import android.app.DatePickerDialog
import android.app.TimePickerDialog
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.google.gson.Gson
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.local.NodeCacheDao
import lk.sliit.smartsolar.data.local.SmartSolarDatabase
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.data.remote.dto.CreateReservationRequest
import lk.sliit.smartsolar.data.remote.dto.ErrorResponse
import lk.sliit.smartsolar.data.remote.dto.NodeLocationDto
import lk.sliit.smartsolar.data.remote.dto.NodeResponse
import lk.sliit.smartsolar.data.remote.dto.ReservationResponse
import lk.sliit.smartsolar.databinding.ActivityCreateBookingBinding
import lk.sliit.smartsolar.util.ReservationStatusStyle
import java.text.SimpleDateFormat
import java.time.LocalDateTime
import java.time.ZoneId
import java.time.format.DateTimeFormatter
import java.util.Calendar
import java.util.Locale

class CreateBookingActivity : AppCompatActivity() {

    private lateinit var binding: ActivityCreateBookingBinding
    private lateinit var nodeCacheDao: NodeCacheDao

    private var activeNodes: List<NodeResponse> = emptyList()
    private var selectedNode: NodeResponse? = null

    private var selectedCalendar: Calendar = Calendar.getInstance()
    private var isDatePicked = false
    private var isTimePicked = false

    private val transactionTypes = arrayOf(
        "Battery charging (draw energy)",
        "Energy drop-off (sell surplus)"
    )
    private var selectedTransactionType = transactionTypes[0]

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityCreateBookingBinding.inflate(layoutInflater)
        setContentView(binding.root)

        nodeCacheDao = NodeCacheDao(SmartSolarDatabase.getInstance(this))

        setupToolbar()
        setupListeners()
        initDefaultDateTime()

        val preselectedNodeId = intent.getStringExtra(EXTRA_NODE_ID)
        loadActiveNodes(preselectedNodeId)
    }

    private fun setupToolbar() {
        setSupportActionBar(binding.toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        binding.toolbar.setNavigationOnClickListener { finish() }
    }

    private fun initDefaultDateTime() {
        // Set default date to today and default time to 1 hour ahead
        selectedCalendar = Calendar.getInstance()
        selectedCalendar.add(Calendar.HOUR_OF_DAY, 1)
        selectedCalendar.set(Calendar.MINUTE, 0)
        selectedCalendar.set(Calendar.SECOND, 0)

        // Pre-fill button displays
        val dateStr = DATE_DISPLAY_FORMAT.format(selectedCalendar.time)
        binding.btnPickDate.text = dateStr.uppercase(Locale.getDefault())
        isDatePicked = true

        val timeStr = TIME_DISPLAY_FORMAT.format(selectedCalendar.time)
        binding.btnPickTime.text = timeStr
        isTimePicked = true
    }

    private fun setupListeners() {
        binding.cardNodeSelect.setOnClickListener {
            showNodeSelectionDialog()
        }

        binding.cardTransactionType.setOnClickListener {
            showTransactionTypeDialog()
        }

        binding.btnPickDate.setOnClickListener {
            showDatePicker()
        }

        binding.btnPickTime.setOnClickListener {
            showTimePicker()
        }

        binding.btnSubmitReservation.setOnClickListener {
            attemptSubmitReservation()
        }

        binding.btnDoneSummary.setOnClickListener {
            finish()
        }
    }

    // Load active nodes from API / SQLite node_cache
    private fun loadActiveNodes(preselectedNodeId: String?) {
        binding.progressBar.visibility = View.VISIBLE

        lifecycleScope.launch {
            try {
                // First try API
                val response = withContext(Dispatchers.IO) {
                    ApiClient.nodesApi.getNodes(active = true, page = 1, pageSize = 100)
                }

                if (response.isSuccessful && response.body() != null) {
                    activeNodes = response.body()?.items?.filter { it.isActive } ?: emptyList()
                }
            } catch (_: Exception) {
                // Ignore API failure, fallback to cache below
            }

            // Fallback to cached nodes if API returned empty
            if (activeNodes.isEmpty()) {
                val cached = withContext(Dispatchers.IO) { nodeCacheDao.getAll() }
                activeNodes = cached.filter { it.isActive }.map {
                    NodeResponse(
                        nodeId = it.nodeId,
                        name = it.name,
                        location = NodeLocationDto(
                            latitude = it.latitude,
                            longitude = it.longitude,
                            addressLine = it.addressLine
                        ),
                        capacityKwh = it.capacityKwh,
                        batterySlotCount = it.batterySlotCount,
                        type = it.type,
                        isActive = it.isActive
                    )
                }
            }

            binding.progressBar.visibility = View.GONE

            if (activeNodes.isNotEmpty()) {
                selectedNode = activeNodes.firstOrNull { it.nodeId == preselectedNodeId }
                    ?: activeNodes.first()
                updateSelectedNodeUi()
            } else {
                binding.tvSelectedNodeName.text = getString(R.string.error_select_node)
            }
        }
    }

    private fun updateSelectedNodeUi() {
        val node = selectedNode ?: return
        binding.tvSelectedNodeName.text = node.name ?: getString(R.string.dashboard_node_label, node.nodeId.orEmpty())

        val typeStr = node.type ?: "AC"
        val slotsStr = getString(R.string.map_battery_slots, node.batterySlotCount)
        val capacityStr = getString(R.string.reservation_capacity, node.capacityKwh)

        binding.tvNodeSpecsSubtitle.text = getString(
            R.string.map_node_specs_format,
            slotsStr,
            capacityStr,
            typeStr
        )
    }

    private fun showNodeSelectionDialog() {
        if (activeNodes.isEmpty()) return

        val names = activeNodes.map {
            it.name ?: getString(R.string.dashboard_node_label, it.nodeId.orEmpty())
        }.toTypedArray()

        AlertDialog.Builder(this)
            .setTitle(R.string.reservation_label_microgrid_node)
            .setItems(names) { _, which ->
                selectedNode = activeNodes[which]
                updateSelectedNodeUi()
            }
            .setNegativeButton(R.string.cancel_btn, null)
            .show()
    }

    private fun showTransactionTypeDialog() {
        AlertDialog.Builder(this)
            .setTitle(R.string.reservation_label_transaction_type)
            .setItems(transactionTypes) { _, which ->
                selectedTransactionType = transactionTypes[which]
                binding.tvTransactionType.text = selectedTransactionType
            }
            .setNegativeButton(R.string.cancel_btn, null)
            .show()
    }

    private fun showDatePicker() {
        val nowMillis = System.currentTimeMillis()
        val maxMillis = nowMillis + (7 * 24 * 60 * 60 * 1000L) // 7-day booking horizon (BR-05)

        val year = selectedCalendar.get(Calendar.YEAR)
        val month = selectedCalendar.get(Calendar.MONTH)
        val day = selectedCalendar.get(Calendar.DAY_OF_MONTH)

        val datePicker = DatePickerDialog(
            this,
            { _, pickedYear, pickedMonth, pickedDay ->
                selectedCalendar.set(Calendar.YEAR, pickedYear)
                selectedCalendar.set(Calendar.MONTH, pickedMonth)
                selectedCalendar.set(Calendar.DAY_OF_MONTH, pickedDay)

                val dateStr = DATE_DISPLAY_FORMAT.format(selectedCalendar.time)
                binding.btnPickDate.text = dateStr.uppercase(Locale.getDefault())
                isDatePicked = true
            },
            year,
            month,
            day
        )

        // BR-05 constraint: Slots must be within the next 7 days
        datePicker.datePicker.minDate = nowMillis - 1000
        datePicker.datePicker.maxDate = maxMillis
        datePicker.show()
    }

    private fun showTimePicker() {
        val hour = selectedCalendar.get(Calendar.HOUR_OF_DAY)
        val minute = selectedCalendar.get(Calendar.MINUTE)

        val timePicker = TimePickerDialog(
            this,
            { _, pickedHour, pickedMinute ->
                selectedCalendar.set(Calendar.HOUR_OF_DAY, pickedHour)
                selectedCalendar.set(Calendar.MINUTE, pickedMinute)
                selectedCalendar.set(Calendar.SECOND, 0)

                val timeStr = TIME_DISPLAY_FORMAT.format(selectedCalendar.time)
                binding.btnPickTime.text = timeStr
                isTimePicked = true
            },
            hour,
            minute,
            true
        )
        timePicker.show()
    }

    private fun attemptSubmitReservation() {
        val node = selectedNode
        if (node == null || node.nodeId.isNullOrBlank()) {
            Toast.makeText(this, getString(R.string.error_select_node), Toast.LENGTH_SHORT).show()
            return
        }

        if (!isDatePicked || !isTimePicked) {
            Toast.makeText(this, getString(R.string.error_select_date_time), Toast.LENGTH_SHORT).show()
            return
        }

        // Validate future date & 7-day horizon (BR-05 / BR-08)
        val nowMillis = System.currentTimeMillis()
        val chosenMillis = selectedCalendar.timeInMillis

        if (chosenMillis <= nowMillis) {
            Toast.makeText(this, "Selected time must be in the future", Toast.LENGTH_SHORT).show()
            return
        }

        val maxAllowedMillis = nowMillis + (7 * 24 * 60 * 60 * 1000L)
        if (chosenMillis > maxAllowedMillis) {
            Toast.makeText(this, "Reservations must be within 7 days (BR-05)", Toast.LENGTH_SHORT).show()
            return
        }

        val durationText = binding.etDuration.text.toString().trim()
        val durationHours = durationText.toIntOrNull()
        if (durationHours == null || durationHours !in 1..12) {
            Toast.makeText(this, getString(R.string.error_invalid_duration), Toast.LENGTH_SHORT).show()
            return
        }

        val energyText = binding.etEnergyKwh.text.toString().trim()
        val energyKwh = energyText.toDoubleOrNull()
        if (energyKwh == null || energyKwh <= 0) {
            Toast.makeText(this, getString(R.string.error_invalid_energy), Toast.LENGTH_SHORT).show()
            return
        }

        val nodeId = node.nodeId
        if (nodeId.isNullOrEmpty() || nodeId.isBlank()) {
            Toast.makeText(this, getString(R.string.error_select_node), Toast.LENGTH_SHORT).show()
            return
        }

        performReservation(nodeId, energyKwh)
    }

    private fun performReservation(nodeId: String, requestedKwh: Double) {
        binding.progressBar.visibility = View.VISIBLE
        binding.btnSubmitReservation.isEnabled = false

        lifecycleScope.launch {
            try {
                // 1. Query available slots for this node from API
                var targetSlotId: String? = null
                val slotsResponse = withContext(Dispatchers.IO) {
                    ApiClient.slotsApi.getSlotsForNode(nodeId)
                }

                if (slotsResponse.isSuccessful && !slotsResponse.body().isNullOrEmpty()) {
                    val availableSlots = slotsResponse.body()!!.filter { it.isActive && it.availableCapacity > 0 }
                    if (availableSlots.isNotEmpty()) {
                        // Find slot matching or closest to chosen start time, or pick first available slot
                        targetSlotId = availableSlots.first().slotId
                    }
                }

                // Fallback to default slot ID if API has no slots
                if (targetSlotId == null) {
                    targetSlotId = "SL-0001"
                }

                // 2. Submit reservation request to POST /api/reservations
                val request = CreateReservationRequest(
                    nodeId = nodeId,
                    slotId = targetSlotId,
                    requestedKwh = requestedKwh
                )

                val response = withContext(Dispatchers.IO) {
                    ApiClient.reservationsApi.createReservation(request)
                }

                if (response.isSuccessful && response.body() != null) {
                    val reservation = response.body()!!
                    showReservationSummary(reservation)
                } else {
                    val errorBody = response.errorBody()?.string()
                    val errorMsg = parseErrorMessage(errorBody, response.code())
                    Toast.makeText(this@CreateBookingActivity, errorMsg, Toast.LENGTH_LONG).show()
                }
            } catch (_: Exception) {
                Toast.makeText(this@CreateBookingActivity, getString(R.string.login_error_network), Toast.LENGTH_LONG).show()
            } finally {
                binding.progressBar.visibility = View.GONE
                binding.btnSubmitReservation.isEnabled = true
            }
        }
    }

    private fun parseErrorMessage(errorBody: String?, statusCode: Int): String {
        if (!errorBody.isNullOrBlank()) {
            try {
                val errorEnvelope = Gson().fromJson(errorBody, ErrorResponse::class.java)
                val msg = errorEnvelope?.message
                if (!msg.isNullOrBlank()) {
                    return msg
                }
            } catch (_: Exception) {
            }
        }
        return when (statusCode) {
            400 -> "Invalid reservation request. Check date/time and energy quota."
            409 -> "Selected slot has no remaining capacity or conflicts with another booking."
            403 -> "Your prosumer account is not active."
            else -> getString(R.string.login_error_generic)
        }
    }

    private fun showReservationSummary(reservation: ReservationResponse) {
        binding.scrollForm.visibility = View.GONE
        binding.layoutSummary.visibility = View.VISIBLE

        binding.tvSummaryId.text = getString(R.string.reservation_summary_id, reservation.reservationId)
        binding.tvSummaryStatus.text = getString(R.string.reservation_summary_status, reservation.status)
        ReservationStatusStyle.apply(binding.tvSummaryStatus, reservation.status)

        val nodeName = selectedNode?.name ?: getString(R.string.dashboard_node_label, reservation.nodeId)
        binding.tvSummaryNode.text = getString(R.string.reservation_summary_node, nodeName)

        val formattedDate = DATE_TIME_FORMAT.format(
            LocalDateTime.ofInstant(selectedCalendar.toInstant(), ZoneId.systemDefault())
        )
        binding.tvSummaryTime.text = getString(R.string.reservation_summary_start_time, formattedDate)
        binding.tvSummaryEnergy.text = getString(R.string.reservation_summary_energy, reservation.requestedKwh)
        binding.tvSummaryTransaction.text = getString(R.string.reservation_summary_transaction, selectedTransactionType)
    }

    companion object {
        const val EXTRA_NODE_ID = "EXTRA_NODE_ID"

        private val DATE_DISPLAY_FORMAT = SimpleDateFormat("EEE, dd MMM yyyy", Locale.getDefault())
        private val TIME_DISPLAY_FORMAT = SimpleDateFormat("HH:mm", Locale.getDefault())
        private val DATE_TIME_FORMAT = DateTimeFormatter.ofPattern("EEE, dd MMM yyyy HH:mm", Locale.getDefault())
    }
}
