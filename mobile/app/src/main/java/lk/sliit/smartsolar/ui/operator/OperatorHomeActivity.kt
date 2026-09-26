/**
 * File        : OperatorHomeActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.operator
 * Description : Placeholder Operator home screen. Shows a welcome message with
 *               the signed-in operator's name and a logout button. Will be
 *               replaced with the full Operator dashboard (QR scanner entry
 *               point, assigned node, nearby map) once REQ-QRV and REQ-VIEW
 *               are implemented. Logout follows the same pattern as
 *               ProsumerHomeActivity.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.ui.operator

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.databinding.ActivityOperatorHomeBinding
import lk.sliit.smartsolar.ui.auth.LoginActivity
import lk.sliit.smartsolar.util.SessionManager

class OperatorHomeActivity : AppCompatActivity() {

    private lateinit var binding: ActivityOperatorHomeBinding
    private lateinit var sessionManager: SessionManager

    // Inflate the home screen and populate the welcome message.
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityOperatorHomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        // Display the operator's name from the intent extra or SQLite session.
        val displayName = intent.getStringExtra("DISPLAY_NAME")
        if (!displayName.isNullOrBlank()) {
            binding.tvWelcome.text = getString(R.string.welcome_message, displayName)
        } else {
            lifecycleScope.launch {
                val sessionName = sessionManager.getValidSession()?.fullName ?: ""
                binding.tvWelcome.text = getString(R.string.welcome_message, sessionName)
            }
        }

        // Wire the logout button.
        binding.btnLogout.setOnClickListener { performLogout() }
    }

    // Call the logout API, clear local session, and return to login.
    private fun performLogout() {
        lifecycleScope.launch {
            // Best-effort API call — even if it fails, we clear the local session.
            try {
                withContext(Dispatchers.IO) {
                    ApiClient.authApi.logout()
                }
            } catch (_: Exception) {
                // Network failure is fine — we still clear the local session.
            }

            // Clear all SQLite tables (session + all caches) per ADR-004.
            sessionManager.clearSession()

            // Navigate to login with a cleared back stack.
            val intent = Intent(this@OperatorHomeActivity, LoginActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            }
            startActivity(intent)
            finish()
        }
    }
}
