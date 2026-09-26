/**
 * File        : OperatorDashboardResponse.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Response DTO for GET /dashboard/operator (api-contract.md §8,
 *               REQ-VIEW §2.3). pendingCount and approvedFutureCount are rendered
 *               exactly as the API returns them — AC-13 treats a client-side count()
 *               as a hard-coded value, so nothing on this screen recomputes them.
 *
 *               AC-6 (counts scoped to the operator's own node) is enforced server
 *               side: DashboardService resolves the caller from the JWT and derives
 *               the scope from their AssignedNodeIds. The optional ?nodeId= may only
 *               narrow that scope, never widen it (BR-11), so the client sends no
 *               node id at all and the counts arrive already scoped.
 *
 *               scopedNodeIds echoes back which nodes those counts cover — this is
 *               the only place the signed-in operator's assigned nodes are exposed to
 *               the client; neither LoginResponse nor CurrentUserResponse carries them.
 *
 *               todaysReservations reuses DashboardReservation because the API returns
 *               the same ReservationResponse record here as on the prosumer dashboard.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.remote.dto

data class OperatorDashboardResponse(
    val pendingCount: Int = 0,
    val approvedFutureCount: Int = 0,
    val todaysReservations: List<DashboardReservation> = emptyList(),
    val scopedNodeIds: List<String> = emptyList()
)
