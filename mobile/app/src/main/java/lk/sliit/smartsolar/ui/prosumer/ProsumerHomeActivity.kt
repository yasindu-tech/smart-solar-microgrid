/**
 * File        : ProsumerHomeActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.prosumer
 * Description : Placeholder Prosumer home screen. Shows a welcome message with
 *               the signed-in user's name (read from SQLite session) and a
 *               logout button. Will be replaced with the full dashboard
 *               (active/pending counts, next booking, map) once REQ-VIEW is
 *               implemented. Logout clears all four SQLite tables (ADR-004)
 *               and navigates back to LoginActivity with a cleared back stack.
 * Author      : <member name - M1>
 */

package lk.sliit.smartsolar.ui.prosumer

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.databinding.ActivityProsumerHomeBinding
import lk.sliit.smartsolar.ui.auth.LoginActivity
import lk.sliit.smartsolar.util.SessionManager

class ProsumerHomeActivity : AppCompatActivity() {

    private lateinit var binding: ActivityProsumerHomeBinding
    private lateinit var sessionManager: SessionManager

    // Inflate the home screen and populate the welcome message.
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityProsumerHomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        // Display the user's name from the intent extra or SQLite session.
        val displayName = intent.getStringExtra("DISPLAY_NAME")
            ?: sessionManager.getValidSession()?.fullName
            ?: ""
        binding.tvWelcome.text = getString(R.string.welcome_message, displayName)

        // Wire the logout button.
        binding.btnLogout.setOnClickListener { performLogout() }
    }

    // Call the logout API, clear local session, and return to login.
    private fun performLogout() {
        lifecycleScope.launch {
            // Best-effort API call — even if it fails, we clear the local session.
            try {
                val token = sessionManager.getToken()
                if (token != null) {
                    withContext(Dispatchers.IO) {
                        ApiClient.authApi.logout(token)
                    }
                }
            } catch (_: Exception) {
                // Network failure is fine — we still clear the local session.
            }

            // Clear all SQLite tables (session + all caches) per ADR-004.
            sessionManager.clearSession()

            // Navigate to login with a cleared back stack.
            val intent = Intent(this@ProsumerHomeActivity, LoginActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            }
            startActivity(intent)
            finish()
        }
    }
}
