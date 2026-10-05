/**
 * File        : CreateReservationRequest.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Request DTO for creating a new energy transfer reservation
 *               (api-contract.md §6).
 */

package lk.sliit.smartsolar.data.remote.dto

data class CreateReservationRequest(
    val nodeId: String,
    val slotId: String,
    val requestedKwh: Double,
    val nic: String? = null
)
