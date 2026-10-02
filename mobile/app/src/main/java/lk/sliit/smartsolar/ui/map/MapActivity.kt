/**
 * File        : MapActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.map
 * Description : Map of nearby microgrid nodes (REQ-VIEW §2.5, DOM-02).
 *               Renders active nodes as interactive pins on Google Maps.
 *               Loads instantly from local SQLite node_cache for fast display,
 *               reconciles with GET /api/nodes/nearby, displays a nearby nodes list
 *               filtered within 100km, and displays a bottom sheet with node details
 *               when a pin or list item is tapped.
 */

package lk.sliit.smartsolar.ui.map

import android.Manifest
import android.annotation.SuppressLint
import android.content.ActivityNotFoundException
import android.content.Intent
import android.content.pm.PackageManager
import android.location.Location
import android.net.Uri
import android.os.Bundle
import android.util.Log
import android.view.View
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import com.google.android.gms.location.FusedLocationProviderClient
import com.google.android.gms.location.LocationServices
import com.google.android.gms.location.Priority
import com.google.android.gms.tasks.CancellationTokenSource
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.GoogleMap
import com.google.android.gms.maps.OnMapReadyCallback
import com.google.android.gms.maps.SupportMapFragment
import com.google.android.gms.maps.model.BitmapDescriptorFactory
import com.google.android.gms.maps.model.LatLng
import com.google.android.gms.maps.model.Marker
import com.google.android.gms.maps.model.MarkerOptions
import com.google.android.material.bottomsheet.BottomSheetDialog
import com.google.android.material.snackbar.Snackbar
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.R
import lk.sliit.smartsolar.data.local.NodeCacheDao
import lk.sliit.smartsolar.data.local.SmartSolarDatabase
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.data.remote.dto.NodeLocationDto
import lk.sliit.smartsolar.data.remote.dto.NodeResponse
import lk.sliit.smartsolar.databinding.ActivityMapBinding
import lk.sliit.smartsolar.databinding.BottomSheetNodeDetailsBinding
import lk.sliit.smartsolar.ui.prosumer.CreateBookingActivity

class MapActivity : AppCompatActivity(), OnMapReadyCallback {

    private lateinit var binding: ActivityMapBinding
    private lateinit var fusedLocationClient: FusedLocationProviderClient
    private lateinit var nodeCacheDao: NodeCacheDao
    private lateinit var nearbyAdapter: NearbyNodesAdapter
    private val markerMap = mutableMapOf<String, Marker>()

    private var googleMap: GoogleMap? = null

    // Default Colombo coordinates used as fallback (REQ-VIEW AC-12)
    private var activeLat: Double = COLOMBO_LAT
    private var activeLng: Double = COLOMBO_LNG
    private var isRealUserLocation: Boolean = false

    // Tracks offline status for disabling bottom sheet action buttons (AC-11)
    private var isOffline: Boolean = false

    // Registered permission launcher for fine/coarse location
    private val locationPermissionLauncher = registerForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { permissions ->
        val fineGranted = permissions[Manifest.permission.ACCESS_FINE_LOCATION] ?: false
        val coarseGranted = permissions[Manifest.permission.ACCESS_COARSE_LOCATION] ?: false

        if (fineGranted || coarseGranted) {
            resolveLocationAndFetchNodes()
        } else {
            // Permission denied — fall back to Colombo default and prompt (AC-12)
            showLocationPromptSnackbar()
            resolveLocationAndFetchNodes()
        }
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMapBinding.inflate(layoutInflater)
        setContentView(binding.root)

        setSupportActionBar(binding.toolbar)
        supportActionBar?.setDisplayHomeAsUpEnabled(true)
        binding.toolbar.setNavigationOnClickListener { finish() }

        fusedLocationClient = LocationServices.getFusedLocationProviderClient(this)
        nodeCacheDao = NodeCacheDao(SmartSolarDatabase.getInstance(this))

        nearbyAdapter = NearbyNodesAdapter { node ->
            onNearbyNodeSelected(node)
        }
        binding.rvNearbyNodes.adapter = nearbyAdapter

        val mapFragment = supportFragmentManager.findFragmentById(R.id.mapFragment) as SupportMapFragment
        mapFragment.getMapAsync(this)
    }

    override fun onMapReady(map: GoogleMap) {
        googleMap = map
        googleMap?.uiSettings?.isZoomControlsEnabled = true

        // Set initial camera position immediately to default coordinates (Colombo) to prevent starting at (0,0)
        googleMap?.moveCamera(CameraUpdateFactory.newLatLngZoom(LatLng(activeLat, activeLng), DEFAULT_ZOOM))

        // Marker click listener opens bottom sheet with node details
        googleMap?.setOnMarkerClickListener { marker ->
            val node = marker.tag as? NodeResponse
            if (node != null) {
                showNodeDetailsBottomSheet(node)
                true
            } else {
                false
            }
        }

        googleMap?.setOnInfoWindowClickListener { marker ->
            val node = marker.tag as? NodeResponse
            if (node != null) {
                showNodeDetailsBottomSheet(node)
            }
        }

        // 1. Immediately render cached nodes for fast startup
        loadCachedNodes()

        // 2. Check location permissions and request if missing
        if (hasLocationPermission()) {
            resolveLocationAndFetchNodes()
        } else {
            locationPermissionLauncher.launch(
                arrayOf(
                    Manifest.permission.ACCESS_FINE_LOCATION,
                    Manifest.permission.ACCESS_COARSE_LOCATION
                )
            )
        }
    }

    private fun hasLocationPermission(): Boolean {
        val fine = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_FINE_LOCATION)
        val coarse = ContextCompat.checkSelfPermission(this, Manifest.permission.ACCESS_COARSE_LOCATION)
        return fine == PackageManager.PERMISSION_GRANTED || coarse == PackageManager.PERMISSION_GRANTED
    }

    // Fast startup: render cached nodes from SQLite node_cache immediately
    private fun loadCachedNodes() {
        lifecycleScope.launch {
            val cachedRows = nodeCacheDao.getAll()
            if (cachedRows.isNotEmpty()) {
                val cachedNodes = cachedRows.map { row ->
                    NodeResponse(
                        nodeId = row.nodeId,
                        name = row.name,
                        location = NodeLocationDto(
                            latitude = row.latitude,
                            longitude = row.longitude,
                            addressLine = row.addressLine
                        ),
                        capacityKwh = row.capacityKwh,
                        batterySlotCount = row.batterySlotCount,
                        type = row.type,
                        isActive = row.isActive
                    )
                }
                plotMarkersOnMap(cachedNodes)
                updateNearbyNodesList(cachedNodes)

                // Check if cache is stale (> 24 hours old, REQ-VIEW EC-6)
                if (nodeCacheDao.isStale()) {
                    setOfflineState(true)
                }
            }
        }
    }

    // Resolve location (or fallback to Colombo) then trigger nearby nodes API fetch
    @SuppressLint("MissingPermission")
    private fun resolveLocationAndFetchNodes() {
        if (hasLocationPermission()) {
            try {
                googleMap?.isMyLocationEnabled = true
                googleMap?.uiSettings?.isMyLocationButtonEnabled = true
            } catch (e: SecurityException) {
                Log.e(TAG, "Location permission error enabling location layer", e)
            }

            fusedLocationClient.lastLocation.addOnSuccessListener { location ->
                if (location != null) {
                    activeLat = location.latitude
                    activeLng = location.longitude
                    isRealUserLocation = true
                    moveCameraToActiveLocation()
                    fetchNearbyNodes()
                } else {
                    // lastLocation was null (fresh launch or location not cached yet) — request current location fresh
                    val cancellationTokenSource = CancellationTokenSource()
                    fusedLocationClient.getCurrentLocation(Priority.PRIORITY_BALANCED_POWER_ACCURACY, cancellationTokenSource.token)
                        .addOnSuccessListener { currentLoc ->
                            if (currentLoc != null) {
                                activeLat = currentLoc.latitude
                                activeLng = currentLoc.longitude
                                isRealUserLocation = true
                            } else {
                                activeLat = COLOMBO_LAT
                                activeLng = COLOMBO_LNG
                                isRealUserLocation = false
                            }
                            moveCameraToActiveLocation()
                            fetchNearbyNodes()
                        }
                        .addOnFailureListener {
                            activeLat = COLOMBO_LAT
                            activeLng = COLOMBO_LNG
                            isRealUserLocation = false
                            moveCameraToActiveLocation()
                            fetchNearbyNodes()
                        }
                }
            }.addOnFailureListener {
                activeLat = COLOMBO_LAT
                activeLng = COLOMBO_LNG
                isRealUserLocation = false
                moveCameraToActiveLocation()
                fetchNearbyNodes()
            }
        } else {
            activeLat = COLOMBO_LAT
            activeLng = COLOMBO_LNG
            isRealUserLocation = false
            moveCameraToActiveLocation()
            fetchNearbyNodes()
        }
    }

    private fun moveCameraToActiveLocation() {
        val target = LatLng(activeLat, activeLng)
        googleMap?.animateCamera(CameraUpdateFactory.newLatLngZoom(target, DEFAULT_ZOOM))
    }

    // Fetch active nodes from GET /api/nodes?active=true&pageSize=100 on IO dispatcher
    private fun fetchNearbyNodes() {
        binding.progressBar.visibility = View.VISIBLE

        lifecycleScope.launch {
            try {
                val response = withContext(Dispatchers.IO) {
                    ApiClient.nodesApi.getNodes(active = true, page = 1, pageSize = 100)
                }

                if (response.isSuccessful && response.body() != null) {
                    val rawNodes = response.body()?.items ?: emptyList()

                    // Filter out invalid coordinates (REQ-VIEW EC-3) and attach computed distance if location is active
                    val validNodes = rawNodes.filter { isValidCoordinate(it.location?.latitude, it.location?.longitude) }
                        .map { node ->
                            if (node.location?.latitude != null && node.location.longitude != null) {
                                val dist = computeDistanceKm(activeLat, activeLng, node.location.latitude, node.location.longitude)
                                node.copy(distanceKm = dist)
                            } else {
                                node
                            }
                        }

                    // Atomically replace SQLite node_cache so deactivated pins disappear (AC-9, BR-20)
                    nodeCacheDao.replaceCache(validNodes)

                    plotMarkersOnMap(validNodes)
                    updateNearbyNodesList(validNodes)
                    setOfflineState(false)
                } else if (response.code() == 401 || response.code() == 403) {
                    Toast.makeText(this@MapActivity, getString(R.string.login_error_session_expired), Toast.LENGTH_SHORT).show()
                } else {
                    showRefreshFailed()
                }
            } catch (e: Exception) {
                Log.e(TAG, "Failed to fetch nearby nodes from API", e)
                showRefreshFailed()
            } finally {
                binding.progressBar.visibility = View.GONE
            }
        }
    }

    private fun updateNearbyNodesList(nodes: List<NodeResponse>) {
        val nodesWithDistance = nodes.map { node ->
            if (node.distanceKm == null && node.location?.latitude != null && node.location.longitude != null) {
                val dist = computeDistanceKm(activeLat, activeLng, node.location.latitude, node.location.longitude)
                node.copy(distanceKm = dist)
            } else {
                node
            }
        }

        // Filter active nodes within 100 km and sort by distance
        val nearbyWithin100 = nodesWithDistance.filter { node ->
            node.isActive && (node.distanceKm == null || node.distanceKm <= MAX_NEARBY_DISTANCE_KM)
        }.sortedBy { it.distanceKm ?: Double.MAX_VALUE }

        binding.tvNearbyHeader.text = getString(
            R.string.map_nearby_nodes_header,
            nearbyWithin100.size,
            MAX_NEARBY_DISTANCE_KM.toInt()
        )
        nearbyAdapter.submitList(nearbyWithin100)
    }

    private fun onNearbyNodeSelected(node: NodeResponse) {
        val lat = node.location?.latitude
        val lng = node.location?.longitude
        if (lat != null && lng != null) {
            val target = LatLng(lat, lng)
            googleMap?.animateCamera(CameraUpdateFactory.newLatLngZoom(target, TARGET_NODE_ZOOM))
            val marker = node.nodeId?.let { markerMap[it] }
            marker?.showInfoWindow()
        }
    }

    private fun computeDistanceKm(lat1: Double, lng1: Double, lat2: Double, lng2: Double): Double {
        val results = FloatArray(1)
        Location.distanceBetween(lat1, lng1, lat2, lng2, results)
        return (results[0] / 1000.0)
    }

    // Validate coordinate bounds [-90, 90] and [-180, 180] (REQ-VIEW EC-3)
    private fun isValidCoordinate(lat: Double?, lng: Double?): Boolean {
        if (lat == null || lng == null) return false
        return lat in -90.0..90.0 && lng in -180.0..180.0
    }

    // Render nodes as markers on Google Map
    private fun plotMarkersOnMap(nodes: List<NodeResponse>) {
        googleMap?.clear()
        markerMap.clear()

        for (node in nodes) {
            val lat = node.location?.latitude ?: continue
            val lng = node.location?.longitude ?: continue

            val position = LatLng(lat, lng)
            val title = node.name ?: getString(R.string.dashboard_node_label, node.nodeId.orEmpty())
            val snippet = getString(R.string.map_tap_to_reserve)

            val markerOptions = MarkerOptions()
                .position(position)
                .title(title)
                .snippet(snippet)
                .icon(BitmapDescriptorFactory.defaultMarker(BitmapDescriptorFactory.HUE_AZURE))

            val marker = googleMap?.addMarker(markerOptions)
            if (marker != null) {
                marker.tag = node
                node.nodeId?.let { id -> markerMap[id] = marker }
            }
        }
    }

    // Display bottom sheet with node details and "Book Here" button (AC-10)
    private fun showNodeDetailsBottomSheet(node: NodeResponse) {
        val sheetBinding = BottomSheetNodeDetailsBinding.inflate(layoutInflater)
        val dialog = BottomSheetDialog(this)
        dialog.setContentView(sheetBinding.root)

        sheetBinding.tvNodeName.text = node.name ?: getString(R.string.dashboard_node_label, node.nodeId.orEmpty())
        sheetBinding.tvNodeIdChip.text = node.nodeId.orEmpty()
        sheetBinding.tvNodeAddress.text = node.location?.addressLine.orEmpty()

        sheetBinding.tvNodeCapacity.text = getString(R.string.map_capacity, node.capacityKwh)
        sheetBinding.tvNodeBatterySlots.text = getString(R.string.map_battery_slots, node.batterySlotCount)
        sheetBinding.tvNodeType.text = getString(R.string.map_type, node.type ?: "AC")

        // Distance is only shown when real-time user location is active AND online
        if (isRealUserLocation && !isOffline && node.distanceKm != null) {
            sheetBinding.tvNodeDistance.visibility = View.VISIBLE
            sheetBinding.tvNodeDistance.text = getString(R.string.map_distance, node.distanceKm)
        } else {
            sheetBinding.tvNodeDistance.visibility = View.GONE
        }

        // AC-11: Disable action buttons when offline
        sheetBinding.btnBookHere.isEnabled = !isOffline
        sheetBinding.btnBookHere.setOnClickListener {
            dialog.dismiss()
            val intent = Intent(this, CreateBookingActivity::class.java).apply {
                putExtra(CreateBookingActivity.EXTRA_NODE_ID, node.nodeId)
            }
            startActivity(intent)
        }

        // Get Directions button: opens Google Maps for navigation/directions to the selected node's location
        val lat = node.location?.latitude
        val lng = node.location?.longitude
        sheetBinding.btnGetDirections.isEnabled = (lat != null && lng != null)
        sheetBinding.btnGetDirections.setOnClickListener {
            if (lat != null && lng != null) {
                val mapsUri = Uri.parse("https://www.google.com/maps/dir/?api=1&destination=$lat,$lng")
                val mapIntent = Intent(Intent.ACTION_VIEW, mapsUri).apply {
                    setPackage("com.google.android.apps.maps")
                }
                try {
                    startActivity(mapIntent)
                } catch (_: ActivityNotFoundException) {
                    val fallbackIntent = Intent(Intent.ACTION_VIEW, mapsUri)
                    try {
                        startActivity(fallbackIntent)
                    } catch (_: Exception) {
                        Toast.makeText(this, getString(R.string.map_no_maps_app_found), Toast.LENGTH_SHORT).show()
                    }
                }
            } else {
                Toast.makeText(this, getString(R.string.map_location_not_available), Toast.LENGTH_SHORT).show()
            }
        }

        dialog.show()
    }

    private fun showRefreshFailed() {
        setOfflineState(true)
        Toast.makeText(this, getString(R.string.dashboard_refresh_failed), Toast.LENGTH_SHORT).show()
    }

    private fun setOfflineState(offline: Boolean) {
        isOffline = offline
        binding.cardOfflineBanner.visibility = if (offline) View.VISIBLE else View.GONE
    }

    private fun showLocationPromptSnackbar() {
        Snackbar.make(binding.root, R.string.location_prompt_text, Snackbar.LENGTH_LONG)
            .setAction(R.string.location_prompt_action) {
                locationPermissionLauncher.launch(
                    arrayOf(
                        Manifest.permission.ACCESS_FINE_LOCATION,
                        Manifest.permission.ACCESS_COARSE_LOCATION
                    )
                )
            }
            .show()
    }

    companion object {
        private const val TAG = "MapActivity"
        private const val COLOMBO_LAT = 6.9271
        private const val COLOMBO_LNG = 79.8612
        private const val DEFAULT_ZOOM = 12f
        private const val TARGET_NODE_ZOOM = 15f
        private const val MAX_NEARBY_DISTANCE_KM = 50.0
    }
}
