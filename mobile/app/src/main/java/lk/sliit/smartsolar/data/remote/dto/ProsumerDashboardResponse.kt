/**
 * File        : ProsumerDashboardResponse.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Response DTO for GET /dashboard/prosumer (api-contract.md §8,
 *               REQ-VIEW §2.1). activeCount and pendingCount are rendered exactly
 *               as the API returns them — REQ-VIEW AC-13 treats a client-side
 *               count() over a cached list as a hard-coded value, so nothing on
 *               this screen recomputes them. nextReservation is nullable because a
 *               prosumer may have no upcoming reservation at all (EC-1).
 *
 *               Field names mirror the server records exactly (ProsumerDashboardResponse
 *               -> ReservationResponse on the API side); ASP.NET Core serialises them
 *               camelCase, which is what Gson binds to here.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.remote.dto

data class ProsumerDashboardResponse(
    val activeCount: Int = 0,
    val pendingCount: Int = 0,
    val nextReservation: DashboardReservation? = null,
    val recentHistory: List<DashboardReservation> = emptyList()
)

/**
 * One reservation summary as it appears on the dashboard.
 *
 * NOTE: the API returns `nodeId`, not a node display name — there is no node name
 * anywhere in ReservationResponse, so the UI shows the id until either the contract
 * grows a nodeName field or node_cache (ADR-004) is populated to resolve it.
 * `slotStartTime` / `slotEndTime` are ISO-8601 strings; the screen formats them for
 * display, which is presentation only and not a derived business number (AC-13).
 */
data class DashboardReservation(
    val reservationId: String? = null,
    val nic: String? = null,
    val nodeId: String? = null,
    val slotStartTime: String? = null,
    val slotEndTime: String? = null,
    val status: String? = null
)
