/**
 * File        : BookingsActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.prosumer
 * Description : "My Bookings" screen for prosumers. Lists all reservations made by the
 *               prosumer with status filter chips (All/Pending/Approved/Completed/Cancelled)
 *               and search input for Reservation ID or Node Name.
 */

package lk.sliit.smartsolar.ui.prosumer

import android.content.Intent
import android.os.Bundle
import android.view.View
import android.widget.Toast
import androidx.appcompat.app.AppCompatActivity
import androidx.core.widget.doOnTextChanged
import androidx.lifecycle.lifecycleScope
import androidx.recyclerview.widget.LinearLayoutManager
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.local.NodeCacheDao
import lk.sliit.smartsolar.data.local.SmartSolarDatabase
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.data.remote.dto.ReservationResponse
import lk.sliit.smartsolar.databinding.ActivityBookingsBinding
import lk.sliit.smartsolar.ui.auth.LoginActivity
import lk.sliit.smartsolar.util.SessionManager
import java.util.Locale

class BookingsActivity : AppCompatActivity() {

    private lateinit var binding: ActivityBookingsBinding
    private lateinit var sessionManager: SessionManager
    private lateinit var nodeCacheDao: NodeCacheDao
    private lateinit var adapter: BookingsAdapter

    private var allReservations = mutableListOf<ReservationResponse>()
    private var nodeMap = mutableMapOf<String, String>()

    private var selectedStatusFilter: String = FILTER_ALL
    private var searchQuery: String = ""

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityBookingsBinding.inflate(layoutInflater)
        setContentView(binding.root)

        sessionManager = SessionManager(this)
        nodeCacheDao = NodeCacheDao(SmartSolarDatabase.getInstance(this))

        setupToolbar()
        setupRecyclerView()
        setupFiltersAndSearch()

        binding.swipeRefresh.setOnRefreshListener {
            loadData(isPullToRefresh = true)
        }

        loadData(isPullToRefresh = false)
    }

    private fun setupToolbar() {
        setSupportActionBar(binding.toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        binding.toolbar.setNavigationOnClickListener { finish() }
    }

    private fun setupRecyclerView() {
        adapter = BookingsAdapter { reservation, nodeName ->
            val intent = Intent(this, ReservationDetailActivity::class.java).apply {
                putExtra(ReservationDetailActivity.EXTRA_RESERVATION_ID, reservation.reservationId)
                putExtra(ReservationDetailActivity.EXTRA_NODE_NAME, nodeName)
            }
            startActivity(intent)
        }
        binding.rvBookings.layoutManager = LinearLayoutManager(this)
        binding.rvBookings.adapter = adapter
    }

    override fun onResume() {
        super.onResume()
        loadData(isPullToRefresh = false)
    }

    private fun setupFiltersAndSearch() {
        binding.etSearch.doOnTextChanged { text, _, _, _ ->
            searchQuery = text?.toString().orEmpty().trim()
            applyFilterAndSearch()
        }

        binding.chipGroupFilter.setOnCheckedStateChangeListener { _, checkedIds ->
            selectedStatusFilter = if (checkedIds.isEmpty()) {
                FILTER_ALL
            } else {
                when (checkedIds.first()) {
                    R.id.chipFilterPending -> STATUS_PENDING
                    R.id.chipFilterApproved -> STATUS_APPROVED
                    R.id.chipFilterCompleted -> STATUS_COMPLETED
                    R.id.chipFilterCancelled -> STATUS_CANCELLED
                    else -> FILTER_ALL
                }
            }
            applyFilterAndSearch()
        }
    }

    private fun loadData(isPullToRefresh: Boolean) {
        setLoading(loading = true, isPullToRefresh = isPullToRefresh)

        lifecycleScope.launch {
            // Load node names map in parallel or sequence
            loadNodeMap()

            // Fetch prosumer reservations
            try {
                val response = withContext(Dispatchers.IO) {
                    ApiClient.reservationsApi.getMyReservations(pageSize = 100)
                }

                if (response.isSuccessful && response.body() != null) {
                    val pagedData = response.body()!!
                    allReservations.clear()
                    allReservations.addAll(pagedData.items)

                    setOfflineState(offline = false)
                    applyFilterAndSearch()
                } else if (response.code() == 401 || response.code() == 403) {
                    forceLogout()
                    return@launch
                } else {
                    showLoadFailed()
                }
            } catch (_: Exception) {
                showLoadFailed()
            } finally {
                setLoading(loading = false, isPullToRefresh = isPullToRefresh)
            }
        }
    }

    private suspend fun loadNodeMap() {
        try {
            val response = withContext(Dispatchers.IO) {
                ApiClient.nodesApi.getNodes(active = null, page = 1, pageSize = 100)
            }
            if (response.isSuccessful && response.body() != null) {
                val nodes = response.body()!!.items
                nodeMap.clear()
                for (node in nodes) {
                    val id = node.nodeId
                    val name = node.name
                    if (!id.isNullOrBlank() && !name.isNullOrBlank()) {
                        nodeMap[id] = name
                    }
                }
            }
        } catch (_: Exception) {
            // Fallback to SQLite cache if network fetch failed
            val cached = withContext(Dispatchers.IO) { nodeCacheDao.getAll() }
            for (node in cached) {
                if (!node.name.isNullOrBlank()) {
                    nodeMap[node.nodeId] = node.name
                }
            }
        }
    }

    private fun applyFilterAndSearch() {
        var filteredList: List<ReservationResponse> = allReservations

        // 1. Status Filter
        if (selectedStatusFilter != FILTER_ALL) {
            filteredList = filteredList.filter {
                it.status.equals(selectedStatusFilter, ignoreCase = true)
            }
        }

        // 2. Search Query (Reservation ID or Node Name)
        if (searchQuery.isNotBlank()) {
            val query = searchQuery.lowercase(Locale.getDefault())
            filteredList = filteredList.filter { res ->
                val resId = res.reservationId.lowercase(Locale.getDefault())
                val nodeId = res.nodeId.lowercase(Locale.getDefault())
                val nodeName = (nodeMap[res.nodeId] ?: "").lowercase(Locale.getDefault())

                resId.contains(query) || nodeId.contains(query) || nodeName.contains(query)
            }
        }

        adapter.submitList(filteredList, nodeMap)

        if (filteredList.isEmpty()) {
            binding.rvBookings.visibility = View.GONE
            binding.layoutEmpty.visibility = View.VISIBLE
        } else {
            binding.rvBookings.visibility = View.VISIBLE
            binding.layoutEmpty.visibility = View.GONE
        }
    }

    private fun setOfflineState(offline: Boolean) {
        binding.cardOfflineBanner.visibility = if (offline) View.VISIBLE else View.GONE
    }

    private fun showLoadFailed() {
        setOfflineState(offline = true)
        Toast.makeText(this, getString(R.string.bookings_load_failed), Toast.LENGTH_SHORT).show()
        applyFilterAndSearch()
    }

    private fun setLoading(loading: Boolean, isPullToRefresh: Boolean) {
        if (isPullToRefresh) {
            binding.swipeRefresh.isRefreshing = loading
        } else {
            binding.progressBar.visibility = if (loading) View.VISIBLE else View.GONE
        }
    }

    private fun forceLogout() {
        lifecycleScope.launch {
            sessionManager.clearSession()
            val intent = Intent(this@BookingsActivity, LoginActivity::class.java).apply {
                flags = Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
            }
            startActivity(intent)
            finish()
        }
    }

    companion object {
        private const val FILTER_ALL = "ALL"
        private const val STATUS_PENDING = "Pending"
        private const val STATUS_APPROVED = "Approved"
        private const val STATUS_COMPLETED = "Completed"
        private const val STATUS_CANCELLED = "Cancelled"
    }
}
