/**
 * File        : OperatorHomeActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.operator
 * Description : Operator dashboard (REQ-VIEW §2.3). Shows the pending and
 *               approved-future reservation counts, today's schedule for the
 *               operator's nodes, and the entry point to the QR scanner.
 *
 *               AC-6 (counts scoped to the operator's own node, not the whole
 *               system) is satisfied server side. GET /dashboard/operator resolves
 *               the caller from the JWT and bounds every count by that account's
 *               AssignedNodeIds; the optional ?nodeId= may only narrow that scope,
 *               never widen it (BR-11). The Android client therefore sends NO node
 *               id — it has none to send, since neither LoginResponse nor
 *               CurrentUserResponse carries assignedNodeIds — and the response's
 *               scopedNodeIds tells the screen which nodes the figures cover.
 *
 *               AC-13: both counts are bound straight from the response fields. The
 *               today list is never counted to produce a metric; when no API figure
 *               is available the counts render as "—".
 *
 *               A failed fetch keeps whatever is on screen and raises the offline
 *               banner instead of crashing. Pull-to-refresh re-fetches, so the cache
 *               is never authoritative (BR-14).
 *
 *               Logout clears all four SQLite tables (ADR-004) and navigates back to
 *               LoginActivity with a cleared back stack.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.ui.operator

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
import androidx.recyclerview.widget.LinearLayoutManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.data.remote.dto.OperatorDashboardResponse
import lk.sliit.smartsolar.databinding.ActivityOperatorHomeBinding
import lk.sliit.smartsolar.databinding.PopupProfileMenuBinding
import lk.sliit.smartsolar.ui.auth.LoginActivity
import lk.sliit.smartsolar.util.SessionManager

class OperatorHomeActivity : AppCompatActivity() {

    private lateinit var binding: ActivityOperatorHomeBinding
    private lateinit var sessionManager: SessionManager
    private lateinit var todaysAdapter: TodaysReservationsAdapter

    // Identity shown in the header greeting and the account dropdown.
    private var profileName: String = ""
    private var profileUserId: String = ""

    // Inflate the dashboard, populate the welcome header and wire actions.
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityOperatorHomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)

        loadIdentity()

        // Today's schedule list.
        todaysAdapter = TodaysReservationsAdapter()
        binding.rvTodaysReservations.layoutManager = LinearLayoutManager(this)
        binding.rvTodaysReservations.adapter = todaysAdapter

        // The avatar in the hero opens the account dropdown (details, logout).
        binding.btnProfileMenu.setOnClickListener { showProfileMenu() }

        binding.btnScanQr.setOnClickListener {
            // TODO: Navigate to QrScanActivity (D4)
        }

        // Pull-to-refresh re-fetches from the API — the cache never decides (BR-14).
        binding.swipeRefresh.setOnRefreshListener { loadDashboard(isPullToRefresh = true) }
    }

    // Refresh on every return so an approval made elsewhere is reflected.
    override fun onResume() {
        super.onResume()
        loadDashboard(isPullToRefresh = false)
    }

    // Resolve the signed-in operator's name and user id for the greeting and dropdown.
    private fun loadIdentity() {
        val extraName = intent.getStringExtra("DISPLAY_NAME")
        if (!extraName.isNullOrBlank()) {
            profileName = extraName
            binding.tvWelcome.text = getString(R.string.welcome_message, extraName)
        }

        lifecycleScope.launch {
            val session = sessionManager.getValidSession()
            profileUserId = session?.nicOrUserId.orEmpty()

            if (profileName.isBlank()) {
                profileName = session?.fullName.orEmpty()
                binding.tvWelcome.text = getString(R.string.welcome_message, profileName)
            }
        }
    }

    // Fetch the dashboard from the API and render it. Mirrors the try/catch and
    // loading-state pattern used by LoginActivity.attemptLogin().
    private fun loadDashboard(isPullToRefresh: Boolean) {
        setLoading(loading = true, isPullToRefresh = isPullToRefresh)

        lifecycleScope.launch {
            try {
                // Call GET /dashboard/operator on the IO dispatcher. No nodeId is sent —
                // the server scopes by the caller's assigned nodes (AC-6, BR-11).
                val response = withContext(Dispatchers.IO) {
                    ApiClient.dashboardApi.getOperatorDashboard()
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
                // Network error — never crash, fall back and flag the data as stale.
                showRefreshFailed()
            } finally {
                setLoading(loading = false, isPullToRefresh = isPullToRefresh)
            }
        }
    }

    // Bind the API response. Counts come from the response fields only (AC-13).
    private fun renderDashboard(dashboard: OperatorDashboardResponse) {
        binding.tvPendingCount.text = dashboard.pendingCount.toString()
        binding.tvApprovedCount.text = dashboard.approvedFutureCount.toString()

        // AC-6: show which nodes these figures cover, straight from the response.
        binding.tvScopedNodes.text = if (dashboard.scopedNodeIds.isEmpty()) {
            getString(R.string.operator_scope_none)
        } else {
            getString(R.string.operator_scope_nodes, dashboard.scopedNodeIds.joinToString(", "))
        }

        // Today's schedule, with an empty state rather than a blank card.
        val hasReservations = dashboard.todaysReservations.isNotEmpty()
        todaysAdapter.submit(dashboard.todaysReservations)
        binding.rvTodaysReservations.visibility = if (hasReservations) View.VISIBLE else View.GONE
        binding.tvTodayEmpty.visibility = if (hasReservations) View.GONE else View.VISIBLE
    }

    // Show the account dropdown anchored under the hero avatar.
    private fun showProfileMenu() {
        val menuBinding = PopupProfileMenuBinding.inflate(layoutInflater)
        menuBinding.tvMenuName.text = profileName
        menuBinding.tvMenuRole.text = getString(R.string.role_operator)
        menuBinding.tvMenuNic.text = profileUserId

        val popup = PopupWindow(
            menuBinding.root,
            ViewGroup.LayoutParams.WRAP_CONTENT,
            ViewGroup.LayoutParams.WRAP_CONTENT,
            true
        )
        // A non-null background is what lets a tap outside dismiss the popup.
        popup.setBackgroundDrawable(ColorDrawable(Color.TRANSPARENT))

        // The operator has no profile screen of its own yet — that belongs to a
        // different requirement slice, so only logout is offered here.
        menuBinding.btnMenuProfile.visibility = View.GONE

        menuBinding.btnMenuLogout.setOnClickListener {
            popup.dismiss()
            performLogout()
        }

        popup.showAsDropDown(binding.btnProfileMenu, 0, 0, Gravity.END)
    }

    // Show the offline banner and flag the refresh failure without losing the screen.
    private fun showRefreshFailed() {
        setOfflineState(offline = true)
        Toast.makeText(this, getString(R.string.dashboard_refresh_failed), Toast.LENGTH_SHORT).show()

        // TODO (ADR-004): render the last-synced rows from the reservation_cache table when a
        //  ReservationCacheDao exists — the table is declared in SmartSolarDatabase but no DAO
        //  reads it yet. The cache stores reservation rows only, with no pending/approved count
        //  columns, so cached counts cannot be reconstructed without a client-side count() —
        //  which AC-13 forbids. Until then the counts stay "—" when no API figure has arrived.
    }

    // Toggle the offline banner and the action buttons along with it.
    private fun setOfflineState(offline: Boolean) {
        binding.cardOfflineBanner.visibility = if (offline) View.VISIBLE else View.GONE

        // Scanning submits a verification to the API, so it is unavailable offline.
        binding.btnScanQr.isEnabled = !offline
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
        val intent = Intent(this@OperatorHomeActivity, LoginActivity::class.java).apply {
            flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        }
        startActivity(intent)
        finish()
    }
}
