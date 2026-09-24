/**
 * File        : ProfileActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.prosumer
 * Description : Displays the logged-in Prosumer's profile. Pulls from API by
 *               default, falling back to SQLite cache if offline (BR-14, REQ-VIEW EC-6).
 *               Allows navigation to Edit or Account Deactivation.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.ui.prosumer

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import com.google.android.material.dialog.MaterialAlertDialogBuilder
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
import lk.sliit.smartsolar.databinding.ActivityProfileBinding
import lk.sliit.smartsolar.ui.auth.LoginActivity
import lk.sliit.smartsolar.util.SessionManager
import java.time.Instant

class ProfileActivity : AppCompatActivity() {

    private lateinit var binding: ActivityProfileBinding
    private lateinit var sessionManager: SessionManager
    private lateinit var profileCacheDao: ProfileCacheDao

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityProfileBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)
        val db = SmartSolarDatabase(this)
        profileCacheDao = ProfileCacheDao(db)

        binding.btnEditProfile.setOnClickListener {
            startActivity(Intent(this, ProfileEditActivity::class.java))
        }

        binding.btnDeactivate.setOnClickListener {
            confirmDeactivation()
        }
    }

    override fun onResume() {
        super.onResume()
        loadProfileData()
    }

    private fun loadProfileData() {
        val session = sessionManager.getValidSession()
        if (session == null) {
            forceLogout()
            return
        }

        val nic = session.nicOrUserId
        
        // 1. Instant render from cache
        val cachedProfile = profileCacheDao.get(nic)
        if (cachedProfile != null) {
            renderProfile(cachedProfile)
            if (profileCacheDao.isStale(nic)) {
                binding.cardOfflineBanner.visibility = View.VISIBLE
            }
        }

        // 2. Fetch fresh data from API
        binding.progressBar.visibility = View.VISIBLE
        binding.btnEditProfile.isEnabled = false
        binding.btnDeactivate.isEnabled = false

        lifecycleScope.launch {
            try {
                val token = sessionManager.getToken() ?: return@launch
                val response = withContext(Dispatchers.IO) {
                    ApiClient.prosumersApi.getProfile(token, nic)
                }

                if (response.isSuccessful && response.body() != null) {
                    val profile = response.body()!!
                    
                    // Update cache
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
                    
                    // Render fresh data and hide offline banner
                    renderProfile(cacheRow)
                    binding.cardOfflineBanner.visibility = View.GONE
                } else if (response.code() == 401 || response.code() == 403) {
                    forceLogout()
                } else {
                    // API error but we might have cache, so just show a toast
                    Toast.makeText(this@ProfileActivity, "Failed to refresh profile", Toast.LENGTH_SHORT).show()
                    binding.cardOfflineBanner.visibility = View.VISIBLE
                }
            } catch (e: Exception) {
                // Network error, show offline banner
                binding.cardOfflineBanner.visibility = View.VISIBLE
            } finally {
                binding.progressBar.visibility = View.GONE
                binding.btnEditProfile.isEnabled = true
                binding.btnDeactivate.isEnabled = true
            }
        }
    }

    private fun renderProfile(profile: ProfileCacheRow) {
        binding.tvNic.text = profile.nic
        binding.tvFullName.text = profile.fullName
        binding.tvEmail.text = profile.email
        binding.tvPhone.text = profile.phone
        binding.tvAddress.text = profile.address
        binding.tvSolarCapacity.text = profile.solarCapacityKw?.toString() ?: ""
        binding.tvStatus.text = profile.status

        // If status is not Active, grey out actions
        val isActive = profile.status == "Active"
        binding.btnEditProfile.isEnabled = isActive
        binding.btnDeactivate.isEnabled = isActive
    }

    private fun confirmDeactivation() {
        MaterialAlertDialogBuilder(this)
            .setTitle(R.string.deactivate_dialog_title)
            .setMessage(R.string.deactivate_dialog_message)
            .setPositiveButton(R.string.deactivate_dialog_confirm) { _, _ ->
                performDeactivation()
            }
            .setNegativeButton(R.string.cancel_btn, null)
            .show()
    }

    private fun performDeactivation() {
        val session = sessionManager.getValidSession() ?: return
        val nic = session.nicOrUserId
        
        binding.progressBar.visibility = View.VISIBLE
        binding.btnDeactivate.isEnabled = false

        lifecycleScope.launch {
            try {
                val token = sessionManager.getToken() ?: return@launch
                val response = withContext(Dispatchers.IO) {
                    ApiClient.prosumersApi.deactivate(token, nic)
                }

                if (response.isSuccessful) {
                    Toast.makeText(this@ProfileActivity, "Account deactivated successfully", Toast.LENGTH_LONG).show()
                    forceLogout()
                } else {
                    val errorBody = response.errorBody()?.string()
                    try {
                        val error = Gson().fromJson(errorBody, ErrorResponse::class.java)
                        Toast.makeText(this@ProfileActivity, error.message, Toast.LENGTH_LONG).show()
                    } catch (_: Exception) {
                        Toast.makeText(this@ProfileActivity, "Failed to deactivate account", Toast.LENGTH_LONG).show()
                    }
                    binding.btnDeactivate.isEnabled = true
                }
            } catch (e: Exception) {
                Toast.makeText(this@ProfileActivity, getString(R.string.login_error_network), Toast.LENGTH_LONG).show()
                binding.btnDeactivate.isEnabled = true
            } finally {
                binding.progressBar.visibility = View.GONE
            }
        }
    }

    private fun forceLogout() {
        sessionManager.clearSession()
        val intent = Intent(this, LoginActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        startActivity(intent)
        finish()
    }
}
