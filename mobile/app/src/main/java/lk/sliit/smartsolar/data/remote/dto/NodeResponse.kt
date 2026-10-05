/**
 * File        : NodeResponse.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Response DTO for microgrid nodes returned by GET /api/nodes/nearby
 *               or GET /api/nodes/{nodeId} (api-contract.md §4).
 */

package lk.sliit.smartsolar.data.remote.dto

data class NodeResponse(
    val nodeId: String? = null,
    val name: String? = null,
    val location: NodeLocationDto? = null,
    val capacityKwh: Double = 0.0,
    val batterySlotCount: Int = 0,
    val type: String? = null,
    val isActive: Boolean = true,
    val distanceKm: Double? = null
)
