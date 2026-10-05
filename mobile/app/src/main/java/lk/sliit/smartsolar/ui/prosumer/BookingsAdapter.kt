/**
 * File        : BookingsAdapter.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.prosumer
 * Description : RecyclerView adapter for rendering prosumer reservations on the
 *               "My Bookings" screen. Formats date, time, requested energy, status chip,
 *               and maps nodeId to the microgrid node name.
 */

package lk.sliit.smartsolar.ui.prosumer

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.remote.dto.ReservationResponse
import lk.sliit.smartsolar.databinding.ItemBookingBinding
import lk.sliit.smartsolar.util.ReservationStatusStyle
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

class BookingsAdapter(
    private val onItemClick: ((ReservationResponse, String?) -> Unit)? = null
) : RecyclerView.Adapter<BookingsAdapter.BookingViewHolder>() {

    private val items = mutableListOf<ReservationResponse>()
    private val nodeNames = mutableMapOf<String, String>()

    fun submitList(newList: List<ReservationResponse>, nodes: Map<String, String>) {
        items.clear()
        items.addAll(newList)
        nodeNames.clear()
        nodeNames.putAll(nodes)
        notifyDataSetChanged()
    }

    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): BookingViewHolder {
        val binding = ItemBookingBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )
        return BookingViewHolder(binding)
    }

    override fun onBindViewHolder(holder: BookingViewHolder, position: Int) {
        val reservation = items[position]
        holder.bind(reservation, nodeNames)
        holder.itemView.setOnClickListener {
            val nodeName = nodeNames[reservation.nodeId]
            onItemClick?.invoke(reservation, nodeName)
        }
    }

    override fun getItemCount(): Int = items.size

    class BookingViewHolder(
        private val binding: ItemBookingBinding
    ) : RecyclerView.ViewHolder(binding.root) {

        fun bind(reservation: ReservationResponse, nodeNames: Map<String, String>) {
            val context = binding.root.context

            binding.tvReservationId.text = reservation.reservationId

            val nodeDisplayName = nodeNames[reservation.nodeId]
                ?: context.getString(R.string.dashboard_node_label, reservation.nodeId)
            binding.tvNodeName.text = nodeDisplayName

            val start = parseApiDateTime(reservation.slotStartTime)
            val end = parseApiDateTime(reservation.slotEndTime)

            val dateStr = start?.format(DATE_FORMAT).orEmpty()
            val timeRangeStr = when {
                start != null && end != null ->
                    "${start.format(TIME_FORMAT)} – ${end.format(TIME_FORMAT)}"
                start != null -> start.format(TIME_FORMAT)
                else -> ""
            }

            binding.tvSlotTime.text = if (dateStr.isNotBlank() && timeRangeStr.isNotBlank()) {
                "$dateStr • $timeRangeStr"
            } else {
                dateStr + timeRangeStr
            }

            binding.tvEnergy.text = context.getString(
                R.string.bookings_energy_format,
                reservation.requestedKwh
            )

            ReservationStatusStyle.apply(binding.tvStatus, reservation.status)
        }

        private fun parseApiDateTime(value: String?): LocalDateTime? {
            if (value.isNullOrBlank()) return null
            return try {
                OffsetDateTime.parse(value).toLocalDateTime()
            } catch (_: Exception) {
                try {
                    LocalDateTime.parse(value)
                } catch (_: Exception) {
                    null
                }
            }
        }

        companion object {
            private val DATE_FORMAT = DateTimeFormatter.ofPattern("EEE, d MMM yyyy", Locale.getDefault())
            private val TIME_FORMAT = DateTimeFormatter.ofPattern("HH:mm", Locale.getDefault())
        }
    }
}
