package lk.sliit.smartsolar.ui.prosumer

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
import lk.sliit.smartsolar.data.local.ProfileCacheDao
import lk.sliit.smartsolar.data.local.ProfileCacheRow
import lk.sliit.smartsolar.data.local.SmartSolarDatabase
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.data.remote.dto.ErrorResponse
import lk.sliit.smartsolar.data.remote.dto.UpdateProsumerRequest
import lk.sliit.smartsolar.databinding.ActivityProfileEditBinding
import lk.sliit.smartsolar.util.SessionManager
import java.time.Instant

class ProfileEditActivity : AppCompatActivity() {

    private lateinit var binding: ActivityProfileEditBinding
    private lateinit var sessionManager: SessionManager
    private lateinit var profileCacheDao: ProfileCacheDao

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityProfileEditBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)
        val db = SmartSolarDatabase(this)
        profileCacheDao = ProfileCacheDao(db)

        populateFormFromCache()

        binding.btnSave.setOnClickListener { saveProfile() }
        binding.btnCancel.setOnClickListener { finish() }
    }

    private fun populateFormFromCache() {
        val session = sessionManager.getValidSession() ?: return
        val cachedProfile = profileCacheDao.get(session.nicOrUserId) ?: return

        binding.etFullName.setText(cachedProfile.fullName)
        binding.etEmail.setText(cachedProfile.email)
        binding.etPhone.setText(cachedProfile.phone)
        binding.etAddress.setText(cachedProfile.address)
        if (cachedProfile.solarCapacityKw != null) {
            binding.etSolarCapacity.setText(cachedProfile.solarCapacityKw.toString())
        }
    }

    private fun saveProfile() {
        // Clear errors
        binding.tvError.visibility = View.GONE
        binding.tilFullName.error = null
        binding.tilEmail.error = null
        binding.tilPhone.error = null
        binding.tilAddress.error = null
        binding.tilSolarCapacity.error = null

        val fullName = binding.etFullName.text.toString().trim()
        val email = binding.etEmail.text.toString().trim()
        val phone = binding.etPhone.text.toString().trim()
        val address = binding.etAddress.text.toString().trim()
        val capacityStr = binding.etSolarCapacity.text.toString().trim()

        var isValid = true
        if (fullName.isEmpty()) { binding.tilFullName.error = getString(R.string.register_error_empty); isValid = false }
        if (email.isEmpty()) { binding.tilEmail.error = getString(R.string.register_error_empty); isValid = false }
        if (phone.isEmpty()) { binding.tilPhone.error = getString(R.string.register_error_empty); isValid = false }
        if (address.isEmpty()) { binding.tilAddress.error = getString(R.string.register_error_empty); isValid = false }
        
        val capacity = capacityStr.toDoubleOrNull()
        if (capacity == null || capacity <= 0) {
            binding.tilSolarCapacity.error = getString(R.string.register_error_empty)
            isValid = false
        }

        if (!isValid) return

        val request = UpdateProsumerRequest(
            fullName = fullName,
            email = email,
            phone = phone,
            address = address,
            solarCapacityKw = capacity!!
        )

        setLoading(true)
        val session = sessionManager.getValidSession() ?: return

        lifecycleScope.launch {
            try {
                val token = sessionManager.getToken() ?: return@launch
                val response = withContext(Dispatchers.IO) {
                    ApiClient.prosumersApi.updateProfile(token, session.nicOrUserId, request)
                }

                if (response.isSuccessful && response.body() != null) {
                    val profile = response.body()!!
                    
                    // Update SQLite cache with the new data
                    val cacheRow = ProfileCacheRow(
                        nic = profile.nic,
                        fullName = profile.fullName,
                        email = profile.email,
                        phone = profile.phone,
                        address = profile.address,
                        solarCapacityKw = profile.solarCapacityKw,
                        status = profile.status,
                        syncedAt = Instant.now().epochSecond
                    )
                    profileCacheDao.save(cacheRow)

                    Toast.makeText(this@ProfileEditActivity, "Profile updated", Toast.LENGTH_SHORT).show()
                    finish() // Return to Profile view
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
            showError(error.message ?: getString(R.string.login_error_generic))
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
        binding.btnSave.isEnabled = !loading
        binding.btnCancel.isEnabled = !loading
        binding.etFullName.isEnabled = !loading
        binding.etEmail.isEnabled = !loading
        binding.etPhone.isEnabled = !loading
        binding.etAddress.isEnabled = !loading
        binding.etSolarCapacity.isEnabled = !loading
    }
}
