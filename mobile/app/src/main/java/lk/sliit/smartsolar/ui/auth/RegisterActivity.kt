package lk.sliit.smartsolar.ui.auth

import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.google.gson.Gson
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.data.remote.dto.ErrorResponse
import lk.sliit.smartsolar.data.remote.dto.RegisterProsumerRequest
import lk.sliit.smartsolar.databinding.ActivityRegisterBinding

class RegisterActivity : AppCompatActivity() {

    private lateinit var binding: ActivityRegisterBinding

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityRegisterBinding.inflate(layoutInflater)
        setContentView(binding.root)

        binding.btnRegister.setOnClickListener { attemptRegistration() }
        binding.btnBackToLogin.setOnClickListener { finish() }
    }

    private fun attemptRegistration() {
        // Clear previous errors
        binding.tvError.visibility = View.GONE
        binding.tilNic.error = null
        binding.tilFullName.error = null
        binding.tilEmail.error = null
        binding.tilPhone.error = null
        binding.tilAddress.error = null
        binding.tilSolarCapacity.error = null
        binding.tilPassword.error = null
        binding.tilConfirmPassword.error = null

        val nic = binding.etNic.text.toString().trim()
        val fullName = binding.etFullName.text.toString().trim()
        val email = binding.etEmail.text.toString().trim()
        val phone = binding.etPhone.text.toString().trim()
        val address = binding.etAddress.text.toString().trim()
        val capacityStr = binding.etSolarCapacity.text.toString().trim()
        val password = binding.etPassword.text.toString()
        val confirmPassword = binding.etConfirmPassword.text.toString()

        var isValid = true

        // Basic validations
        if (nic.isEmpty()) { binding.tilNic.error = getString(R.string.register_error_empty); isValid = false }
        else if (!nic.matches(Regex("^([0-9]{9}[vVxX]|[0-9]{12})$"))) {
            binding.tilNic.error = getString(R.string.register_error_nic)
            isValid = false
        }

        if (fullName.isEmpty()) { binding.tilFullName.error = getString(R.string.register_error_empty); isValid = false }
        if (email.isEmpty()) { binding.tilEmail.error = getString(R.string.register_error_empty); isValid = false }
        if (phone.isEmpty()) { binding.tilPhone.error = getString(R.string.register_error_empty); isValid = false }
        if (address.isEmpty()) { binding.tilAddress.error = getString(R.string.register_error_empty); isValid = false }
        
        val capacity = capacityStr.toDoubleOrNull()
        if (capacity == null || capacity <= 0) {
            binding.tilSolarCapacity.error = getString(R.string.register_error_empty)
            isValid = false
        }

        if (password.isEmpty()) { binding.tilPassword.error = getString(R.string.register_error_empty); isValid = false }
        if (password != confirmPassword) {
            binding.tilConfirmPassword.error = getString(R.string.register_error_password_match)
            isValid = false
        }

        if (!isValid) return

        val request = RegisterProsumerRequest(
            nic = nic,
            fullName = fullName,
            email = email,
            phone = phone,
            address = address,
            password = password,
            solarCapacityKw = capacity!!
        )

        setLoading(true)

        lifecycleScope.launch {
            try {
                val response = withContext(Dispatchers.IO) {
                    ApiClient.prosumersApi.register(request)
                }

                if (response.isSuccessful) {
                    Toast.makeText(this@RegisterActivity, "Account created — awaiting Backoffice approval", Toast.LENGTH_LONG).show()
                    finish() // Return to login screen
                } else {
                    handleApiError(response.errorBody()?.string())
                }
            } catch (e: Exception) {
                showError(getString(R.string.login_error_network))
            } finally {
                setLoading(false)
            }
        }
    }

    private fun handleApiError(errorBody: String?) {
        if (errorBody == null) {
            showError(getString(R.string.login_error_network))
            return
        }

        try {
            val error = Gson().fromJson(errorBody, ErrorResponse::class.java)
            if (error.code == "E4001" && error.message?.contains("NIC") == true) {
                binding.tilNic.error = error.message
            } else {
                showError(error.message ?: getString(R.string.login_error_generic))
            }
        } catch (_: Exception) {
            showError(getString(R.string.login_error_generic))
        }
    }

    private fun showError(message: String) {
        binding.tvError.text = message
        binding.tvError.visibility = View.VISIBLE
    }

    private fun setLoading(loading: Boolean) {
        binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
        binding.btnRegister.isEnabled = !loading
        binding.btnBackToLogin.isEnabled = !loading
        // Disable fields
        binding.etNic.isEnabled = !loading
        binding.etFullName.isEnabled = !loading
        binding.etEmail.isEnabled = !loading
        binding.etPhone.isEnabled = !loading
        binding.etAddress.isEnabled = !loading
        binding.etSolarCapacity.isEnabled = !loading
        binding.etPassword.isEnabled = !loading
        binding.etConfirmPassword.isEnabled = !loading
    }
}
