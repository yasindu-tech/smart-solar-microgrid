/**
 * File        : LoginActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.auth
 * Description : Entry screen. Prosumers sign in with NIC, operators with email.
 *               On launch, checks SQLite for a valid (non-expired) session and
 *               auto-navigates to the role home if found (REQ-AUTH AC-8). Expired
 *               sessions are silently deleted (AC-9). The role claim in the
 *               returned token decides which home screen is shown (AC-3, AC-4).
 *               No business rule is evaluated here — all decisions come from
 *               the API response.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.ui.auth

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.google.gson.Gson
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.data.remote.dto.ErrorResponse
import lk.sliit.smartsolar.data.remote.dto.LoginRequest
import lk.sliit.smartsolar.databinding.ActivityLoginBinding
import lk.sliit.smartsolar.ui.operator.OperatorHomeActivity
import lk.sliit.smartsolar.ui.prosumer.ProsumerHomeActivity
import lk.sliit.smartsolar.util.SessionManager

class LoginActivity : AppCompatActivity() {

    private lateinit var binding: ActivityLoginBinding
    private lateinit var sessionManager: SessionManager

    // Inflate the login screen and check for an existing valid session.
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityLoginBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        // Wire the login button click handler.
        binding.btnLogin.setOnClickListener { attemptLogin() }

        // Wire the register link.
        binding.tvRegisterLink.setOnClickListener {
            startActivity(Intent(this, RegisterActivity::class.java))
        }

        // AC-8: If a non-expired session exists in SQLite, skip login entirely.
        lifecycleScope.launch {
            val existingSession = sessionManager.getValidSession()
            if (existingSession != null) {
                verifySessionAndNavigate(existingSession)
            }
        }
    }

    private fun verifySessionAndNavigate(session: lk.sliit.smartsolar.data.local.SessionRow) {
        setLoading(true)
        lifecycleScope.launch {
            try {
                val response = withContext(Dispatchers.IO) {
                    ApiClient.authApi.me()
                }

                if (response.isSuccessful && response.body() != null) {
                    // Token is valid and account is active
                    navigateToHome(session.role, session.fullName)
                } else if (response.code() == 401 || response.code() == 403) {
                    // EC-6: Account deactivated while token is still valid locally
                    sessionManager.clearSession()
                    showError(getString(R.string.login_error_inactive))
                    setLoading(false)
                } else {
                    // API error (500 etc), let them in anyway based on cache
                    navigateToHome(session.role, session.fullName)
                }
            } catch (e: Exception) {
                // Network error, let them in anyway based on cache (offline mode)
                navigateToHome(session.role, session.fullName)
            }
        }
    }

    // Validate inputs client-side and call the login API.
    private fun attemptLogin() {
        // Clear any previous error.
        binding.tvError.visibility = View.GONE
        binding.tilIdentifier.error = null
        binding.tilPassword.error = null

        val identifier = binding.etIdentifier.text.toString().trim()
        val password = binding.etPassword.text.toString()

        // Client-side validation — API still validates as a backup (EC-5).
        if (identifier.isEmpty()) {
            binding.tilIdentifier.error = getString(R.string.login_error_empty_identifier)
            return
        }
        if (password.isEmpty()) {
            binding.tilPassword.error = getString(R.string.login_error_empty_password)
            return
        }

        // Show loading state and disable the button.
        setLoading(true)

        lifecycleScope.launch {
            try {
                // Call POST /auth/login on the IO dispatcher.
                val response = withContext(Dispatchers.IO) {
                    ApiClient.authApi.login(LoginRequest(identifier, password))
                }

                if (response.isSuccessful && response.body() != null) {
                    val loginResponse = response.body()!!
                    
                    if (loginResponse.role == "Backoffice") {
                        // Backoffice users cannot use the mobile app.
                        showError(getString(R.string.login_error_web_only))
                    } else {
                        // Save session to SQLite and navigate to the role home.
                        sessionManager.saveSession(loginResponse)
                        navigateToHome(loginResponse.role, loginResponse.displayName)
                    }
                } else {
                    // Parse the error envelope from the API response.
                    handleApiError(response.errorBody()?.string())
                }
            } catch (e: Exception) {
                // Network error — no connection, timeout, DNS failure, etc.
                showError(getString(R.string.login_error_network))
            } finally {
                setLoading(false)
            }
        }
    }

    // Parse the API error envelope and show the appropriate message.
    private fun handleApiError(errorBody: String?) {
        if (errorBody == null) {
            showError(getString(R.string.login_error_network))
            return
        }

        try {
            val error = Gson().fromJson(errorBody, ErrorResponse::class.java)
            when (error.code) {
                // E4301: invalid credentials — generic message so we don't reveal
                // whether the account exists (AC-5).
                "E4301" -> showError(error.message ?: getString(R.string.login_error_invalid))

                // E4302: account not active — the API message already distinguishes
                // PendingActivation vs Deactivated (AC-6, AC-7).
                "E4302" -> showError(error.message ?: getString(R.string.login_error_inactive))

                // E4000: empty password — should not reach here if client validates,
                // but handle it gracefully.
                "E4000" -> binding.tilPassword.error = error.message

                // Any other error code — show the message as-is.
                else -> showError(error.message ?: getString(R.string.login_error_generic))
            }
        } catch (_: Exception) {
            // JSON parsing failed — show a generic message.
            showError(getString(R.string.login_error_generic))
        }
    }

    // Display an error message below the form fields.
    private fun showError(message: String) {
        binding.tvError.text = message
        binding.tvError.visibility = View.VISIBLE
    }

    // Toggle loading spinner and button enabled state.
    private fun setLoading(loading: Boolean) {
        binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
        binding.btnLogin.isEnabled = !loading
        binding.etIdentifier.isEnabled = !loading
        binding.etPassword.isEnabled = !loading
    }

    // Navigate to the correct home screen based on the role claim from the JWT.
    private fun navigateToHome(role: String, displayName: String?) {
        val target = when (role) {
            "Prosumer" -> ProsumerHomeActivity::class.java
            "Operator" -> OperatorHomeActivity::class.java
            else -> {
                // Backoffice users should use the web app — show an error.
                showError(getString(R.string.login_error_web_only))
                setLoading(false)
                return
            }
        }

        val intent = Intent(this, target).apply {
            putExtra("DISPLAY_NAME", displayName)
            // Clear the back stack so pressing back doesn't return to login.
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        startActivity(intent)
        finish()
    }
}
