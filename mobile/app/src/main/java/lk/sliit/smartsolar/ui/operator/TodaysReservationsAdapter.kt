/**
 * File        : TodaysReservationsAdapter.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.operator
 * Description : RecyclerView adapter for the operator dashboard's "Today's
 *               reservations" list (REQ-VIEW §2.3). Renders rows exactly as the API
 *               returned them — the adapter derives no counts and makes no decisions,
 *               it only formats the slot time for display.
 *
 *               The API's ReservationResponse carries nic and nodeId but no display
 *               names, so the rows label those ids rather than inventing a lookup.
 */

package lk.sliit.smartsolar.ui.operator

import android.view.LayoutInflater
import android.view.ViewGroup
import androidx.recyclerview.widget.RecyclerView
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.remote.dto.DashboardReservation
import lk.sliit.smartsolar.databinding.ItemOperatorReservationBinding
import lk.sliit.smartsolar.util.ReservationStatusStyle
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

class TodaysReservationsAdapter :
    RecyclerView.Adapter<TodaysReservationsAdapter.ReservationViewHolder>() {

    private val items = mutableListOf<DashboardReservation>()

    // Replace the whole list with the rows from the latest API response.
    fun submit(reservations: List<DashboardReservation>) {
        items.clear()
        items.addAll(reservations)
        notifyDataSetChanged()
    }

    // Inflate one row.
    override fun onCreateViewHolder(parent: ViewGroup, viewType: Int): ReservationViewHolder {
        val binding = ItemOperatorReservationBinding.inflate(
            LayoutInflater.from(parent.context),
            parent,
            false
        )
        return ReservationViewHolder(binding)
    }

    // Bind one reservation to its row.
    override fun onBindViewHolder(holder: ReservationViewHolder, position: Int) {
        holder.bind(items[position])
    }

    // Number of rows currently held. This is list size for RecyclerView's own use only —
    // it is never shown as a dashboard metric (AC-13).
    override fun getItemCount(): Int = items.size

    class ReservationViewHolder(
        private val binding: ItemOperatorReservationBinding
    ) : RecyclerView.ViewHolder(binding.root) {

        // Populate the row: slot time, prosumer, node and status chip.
        fun bind(reservation: DashboardReservation) {
            val context = binding.root.context

            val start = parseApiDateTime(reservation.slotStartTime)
            val end = parseApiDateTime(reservation.slotEndTime)
            binding.tvRowTime.text = when {
                start != null && end != null ->
                    "${start.format(TIME_FORMAT)} – ${end.format(TIME_FORMAT)}"
                start != null -> start.format(TIME_FORMAT)
                else -> ""
            }

            binding.tvRowProsumer.text =
                context.getString(R.string.operator_row_prosumer, reservation.nic.orEmpty())
            binding.tvRowNode.text =
                context.getString(R.string.dashboard_node_label, reservation.nodeId.orEmpty())

            ReservationStatusStyle.apply(binding.tvRowStatus, reservation.status)
        }

        // Parse an ISO-8601 timestamp from the API. ASP.NET Core emits DateTime with or
        // without a trailing offset depending on DateTimeKind, so both shapes are accepted.
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
            private val TIME_FORMAT =
                DateTimeFormatter.ofPattern("HH:mm", Locale.getDefault())
        }
    }
}
