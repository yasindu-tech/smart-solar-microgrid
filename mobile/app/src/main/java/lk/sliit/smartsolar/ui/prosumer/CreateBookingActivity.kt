/**
 * File        : CreateBookingActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.prosumer
 * Description : Booking creation screen (D3). Receives pre-selected nodeId via
 *               EXTRA_NODE_ID when launched from the map screen (REQ-VIEW §2.5 AC-10).
 */

package lk.sliit.smartsolar.ui.prosumer

import android.os.Bundle
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import lk.sliit.smartsolar.R

class CreateBookingActivity : AppCompatActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_profile) // Reuse profile view or placeholder layout

        val nodeId = intent.getStringExtra(EXTRA_NODE_ID)
        if (!nodeId.isNullOrBlank()) {
            Toast.makeText(this, "Booking flow for Node $nodeId", Toast.LENGTH_LONG).show()
        }
    }

    companion object {
        const val EXTRA_NODE_ID = "EXTRA_NODE_ID"
    }
}
