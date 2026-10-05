/**
 * File        : ReservationResponse.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Response DTO representing an energy transfer reservation contract
 *               returned by POST /api/reservations and GET /api/reservations/{id}.
 */

package lk.sliit.smartsolar.data.remote.dto

data class ReservationResponse(
    val reservationId: String,
    val nic: String,
    val nodeId: String,
    val slotId: String,
    val slotStartTime: String,
    val slotEndTime: String,
    val requestedKwh: Double,
    val status: String,
    val qrToken: String? = null,
    val qrIssuedAt: String? = null,
    val createdAt: String? = null,
    val updatedAt: String? = null,
    val cancelledAt: String? = null,
    val cancelledBy: String? = null,
    val completedAt: String? = null,
    val completedByOperatorId: String? = null,
    val deliveredKwh: Double? = null
)
