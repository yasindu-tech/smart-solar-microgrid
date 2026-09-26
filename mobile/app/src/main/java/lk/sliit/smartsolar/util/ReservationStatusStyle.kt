/**
 * File        : ReservationStatusStyle.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : util
 * Description : Single source of truth for how a reservation status is coloured as a
 *               chip. Used by both the prosumer dashboard's next-reservation card and
 *               the operator dashboard's today list, so the two cannot drift apart.
 *
 *               The status strings are the ones the API returns (Pending, Approved,
 *               Cancelled, Completed, Expired); anything unrecognised falls back to a
 *               neutral surface so an added status never renders as an invisible chip.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.util

import android.widget.TextView
import androidx.core.content.ContextCompat
import lk.sliit.smartsolar.R

object ReservationStatusStyle {

    // Set the chip text and paint it from the sg_* semantic tokens for that status.
    fun apply(textView: TextView, status: String?) {
        textView.text = status.orEmpty()

        val (backgroundRes, textRes) = when (status) {
            "Approved" -> R.color.sg_success_soft to R.color.sg_success_text
            "Pending" -> R.color.sg_warning_soft to R.color.sg_warning_text
            "Cancelled" -> R.color.sg_danger_soft to R.color.sg_danger_text
            "Completed" -> R.color.sg_info_soft to R.color.sg_info_text
            else -> R.color.sg_surface_muted to R.color.sg_text_secondary
        }

        val context = textView.context
        textView.backgroundTintList = ContextCompat.getColorStateList(context, backgroundRes)
        textView.setTextColor(ContextCompat.getColor(context, textRes))
    }
}
