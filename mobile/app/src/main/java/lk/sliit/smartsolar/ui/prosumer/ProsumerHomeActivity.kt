/**
 * File        : ProsumerHomeActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.prosumer
 * Description : Prosumer dashboard (REQ-VIEW §2.1). Shows the active/pending
 *               reservation counts, the next upcoming reservation and quick actions.
 *
 *               Every metric is bound straight from GET /dashboard/prosumer — the
 *               screen never counts a cached list, because AC-13 treats a client-side
 *               count() as a hard-coded value. When no API figure is available the
 *               counts render as "—" rather than a locally derived number.
 *
 *               A failed fetch falls back to whatever is already on screen and raises
 *               the offline banner instead of crashing (EC-4, EC-6), and disables the
 *               action buttons while offline (AC-11). Pull-to-refresh re-fetches, so
 *               the cache is never authoritative (BR-14).
 *
 *               The account avatar in the hero opens a dropdown carrying the signed-in
 *               user's details plus the profile and logout actions, so neither needs a
 *               permanent slot on the dashboard body.
 *
 *               Logout clears all four SQLite tables (ADR-004) and navigates back to
 *               LoginActivity with a cleared back stack.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.ui.prosumer

import android.content.Intent
import android.graphics.Color
import android.graphics.drawable.ColorDrawable
import android.os.Bundle
import android.view.Gravity
import android.view.View
import android.view.ViewGroup
import android.widget.PopupWindow
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.lifecycle.lifecycleScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.data.remote.dto.DashboardReservation
import lk.sliit.smartsolar.data.remote.dto.ProsumerDashboardResponse
import lk.sliit.smartsolar.databinding.ActivityProsumerHomeBinding
import lk.sliit.smartsolar.databinding.PopupProfileMenuBinding
import lk.sliit.smartsolar.ui.auth.LoginActivity
import lk.sliit.smartsolar.ui.map.MapActivity
import lk.sliit.smartsolar.util.ReservationStatusStyle
import lk.sliit.smartsolar.util.SessionManager
import java.time.LocalDateTime
import java.time.OffsetDateTime
import java.time.format.DateTimeFormatter
import java.util.Locale

class ProsumerHomeActivity : AppCompatActivity() {

    private lateinit var binding: ActivityProsumerHomeBinding
    private lateinit var sessionManager: SessionManager

    // Identity shown in the header greeting and the profile dropdown.
    private var profileName: String = ""
    private var profileNic: String = ""

    // Tracks whether the last fetch failed, so the dropdown can disable its
    // navigation action when it is opened while offline (AC-11).
    private var isOffline: Boolean = false

    // Inflate the dashboard, populate the welcome header and wire navigation.
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityProsumerHomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        loadIdentity()

        // The avatar in the hero opens the account dropdown (details, profile, logout).
        binding.btnProfileMenu.setOnClickListener { showProfileMenu() }

        binding.btnBookings.setOnClickListener {
            startActivity(Intent(this, BookingsActivity::class.java))
        }

        binding.btnMap.setOnClickListener {
            startActivity(Intent(this, MapActivity::class.java))
        }

        binding.btnBookNow.setOnClickListener {
            startActivity(Intent(this, CreateBookingActivity::class.java))
        }

        // Pull-to-refresh re-fetches from the API (EC-4 — the cache never decides).
        binding.swipeRefresh.setOnRefreshListener { loadDashboard(isPullToRefresh = true) }
    }

    // Resolve the signed-in user's name and NIC for the greeting and the dropdown.
    // The name arrives as an intent extra straight after login; the NIC always comes
    // from the SQLite session.
    private fun loadIdentity() {
        val extraName = intent.getStringExtra("DISPLAY_NAME")
        if (!extraName.isNullOrBlank()) {
            profileName = extraName
            binding.tvWelcome.text = getString(R.string.welcome_message, extraName)
        }

        lifecycleScope.launch {
            val session = sessionManager.getValidSession()
            profileNic = session?.nicOrUserId.orEmpty()

            if (profileName.isBlank()) {
                profileName = session?.fullName.orEmpty()
                binding.tvWelcome.text = getString(R.string.welcome_message, profileName)
            }
        }
    }

    // Show the account dropdown anchored under the hero avatar.
    private fun showProfileMenu() {
        val menuBinding = PopupProfileMenuBinding.inflate(layoutInflater)
        menuBinding.tvMenuName.text = profileName
        menuBinding.tvMenuNic.text =
            if (profileNic.isBlank()) "" else getString(R.string.profile_menu_nic, profileNic)

        val popup = PopupWindow(
            menuBinding.root,
            ViewGroup.LayoutParams.WRAP_CONTENT,
            ViewGroup.LayoutParams.WRAP_CONTENT,
            true
        )
        // A non-null background is what lets a tap outside dismiss the popup.
        popup.setBackgroundDrawable(ColorDrawable(Color.TRANSPARENT))

        // AC-11: navigation is disabled while showing stale data; logout is a local
        // action and stays available.
        menuBinding.btnMenuProfile.isEnabled = !isOffline
        menuBinding.btnMenuProfile.setOnClickListener {
            popup.dismiss()
            startActivity(Intent(this, ProfileActivity::class.java))
        }

        menuBinding.btnMenuLogout.setOnClickListener {
            popup.dismiss()
            performLogout()
        }

        popup.showAsDropDown(binding.btnProfileMenu, 0, 0, Gravity.END)
    }

    // Refresh on every return to the screen so a cancel made elsewhere is reflected (AC-2).
    override fun onResume() {
        super.onResume()
        loadDashboard(isPullToRefresh = false)
    }

    // Fetch the dashboard from the API and render it. Mirrors the try/catch and
    // loading-state pattern used by LoginActivity.attemptLogin().
    private fun loadDashboard(isPullToRefresh: Boolean) {
        setLoading(loading = true, isPullToRefresh = isPullToRefresh)

        lifecycleScope.launch {
            try {
                // Call GET /dashboard/prosumer on the IO dispatcher.
                val response = withContext(Dispatchers.IO) {
                    ApiClient.dashboardApi.getProsumerDashboard()
                }

                if (response.isSuccessful && response.body() != null) {
                    renderDashboard(response.body()!!)
                    setOfflineState(offline = false)
                } else if (response.code() == 401 || response.code() == 403) {
                    // Session no longer valid on the server — send the user back to login.
                    forceLogout()
                    return@launch
                } else {
                    // API reachable but unhappy: keep what is on screen, flag it as stale.
                    showRefreshFailed()
                }
            } catch (e: Exception) {
                // Network error — never crash, fall back and flag the data as stale (EC-4/EC-6).
                showRefreshFailed()
            } finally {
                setLoading(loading = false, isPullToRefresh = isPullToRefresh)
            }
        }
    }

    // Bind the API response to the screen. Counts come from the response fields only (AC-13).
    private fun renderDashboard(dashboard: ProsumerDashboardResponse) {
        binding.tvActiveCount.text = dashboard.activeCount.toString()
        binding.tvPendingCount.text = dashboard.pendingCount.toString()

        val next = dashboard.nextReservation
        if (next != null) {
            renderNextReservation(next)
        } else {
            // EC-1: zero reservations gets a call to action, not a blank card.
            binding.layoutNextContent.visibility = View.GONE
            binding.layoutNextEmpty.visibility = View.VISIBLE
        }
    }

    // Populate the next-reservation card: node, date, time range and status chip.
    private fun renderNextReservation(next: DashboardReservation) {
        binding.layoutNextEmpty.visibility = View.GONE
        binding.layoutNextContent.visibility = View.VISIBLE

        binding.cardNextReservation.setOnClickListener {
            val resId = next.reservationId ?: return@setOnClickListener
            val intent = Intent(this, ReservationDetailActivity::class.java).apply {
                putExtra(ReservationDetailActivity.EXTRA_RESERVATION_ID, resId)
            }
            startActivity(intent)
        }

        // The contract returns nodeId only — there is no node name on ReservationResponse.
        binding.tvNextNode.text = getString(R.string.dashboard_node_label, next.nodeId.orEmpty())

        val start = parseApiDateTime(next.slotStartTime)
        val end = parseApiDateTime(next.slotEndTime)

        binding.tvNextDate.text = start?.format(DATE_FORMAT).orEmpty()
        binding.tvNextTime.text = when {
            start != null && end != null ->
                "${start.format(TIME_FORMAT)} – ${end.format(TIME_FORMAT)}"
            start != null -> start.format(TIME_FORMAT)
            else -> ""
        }

        renderStatusChip(next.status)
    }

    // Colour the status chip from the sg_* semantic tokens for the returned status.
    private fun renderStatusChip(status: String?) {
        ReservationStatusStyle.apply(binding.tvNextStatus, status)
    }

    // Show the offline banner and flag the refresh failure without losing what is on screen.
    private fun showRefreshFailed() {
        setOfflineState(offline = true)
        Toast.makeText(this, getString(R.string.dashboard_refresh_failed), Toast.LENGTH_SHORT).show()

        // TODO (ADR-004): render the last-synced rows from the reservation_cache table when a
        //  ReservationCacheDao exists — the table is declared in SmartSolarDatabase but no DAO
        //  reads it yet. Note that reservation_cache stores reservation rows only, with no
        //  activeCount/pendingCount columns, so the cached counts cannot be reconstructed without
        //  a client-side count() — which AC-13 forbids. Restoring counts offline needs either a
        //  counts column on the cache or an agreed exception; until then the counts stay "—".
    }

    // Toggle the offline banner and, per AC-11, the action buttons along with it.
    private fun setOfflineState(offline: Boolean) {
        isOffline = offline
        binding.cardOfflineBanner.visibility = if (offline) View.VISIBLE else View.GONE

        // AC-11: all action buttons are disabled while showing cached/stale data. The
        // profile action now lives in the dropdown, which reads isOffline when it opens.
        binding.btnBookings.isEnabled = !offline
        binding.btnMap.isEnabled = !offline
        binding.btnBookNow.isEnabled = !offline
    }

    // Toggle the loading indicator. Pull-to-refresh draws its own spinner, so the
    // inline ProgressBar is only used for the initial load.
    private fun setLoading(loading: Boolean, isPullToRefresh: Boolean) {
        if (isPullToRefresh) {
            binding.swipeRefresh.isRefreshing = loading
        } else {
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
        }
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

    // Clear the local session and return to login when the server rejects the token.
    private fun forceLogout() {
        lifecycleScope.launch {
            sessionManager.clearSession()
            navigateToLogin()
        }
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

            navigateToLogin()
        }
    }

    // Navigate to login with a cleared back stack.
    private fun navigateToLogin() {
        val intent = Intent(this@ProsumerHomeActivity, LoginActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        startActivity(intent)
        finish()
    }

    companion object {
        private val DATE_FORMAT = DateTimeFormatter.ofPattern("EEE, d MMM yyyy", Locale.getDefault())
        private val TIME_FORMAT = DateTimeFormatter.ofPattern("HH:mm", Locale.getDefault())
    }
}
