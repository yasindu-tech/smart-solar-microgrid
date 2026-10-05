/**
 * File        : SlotResponse.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Response DTO representing an energy booking slot returned by
 *               GET /api/nodes/{nodeId}/slots (api-contract.md §5).
 */

package lk.sliit.smartsolar.data.remote.dto

data class SlotResponse(
    val slotId: String,
    val nodeId: String,
    val startTime: String,
    val endTime: String,
    val totalCapacity: Int,
    val availableCapacity: Int,
    val energyOfferedKwh: Double,
    val isActive: Boolean = true,
    val createdAt: String? = null,
    val updatedAt: String? = null
)
