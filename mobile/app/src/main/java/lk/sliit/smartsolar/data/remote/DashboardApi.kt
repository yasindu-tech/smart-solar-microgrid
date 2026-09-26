/**
 * File        : DashboardApi.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote
 * Description : Retrofit service interface for the /dashboard endpoints defined in
 *               api-contract.md §8. Only the prosumer dashboard is bound here; the
 *               operator and backoffice dashboards are separate requirement slices.
 *               Every metric on the screen comes from this response (REQ-VIEW AC-13).
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.remote

import lk.sliit.smartsolar.data.remote.dto.OperatorDashboardResponse
import lk.sliit.smartsolar.data.remote.dto.ProsumerDashboardResponse
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Query

interface DashboardApi {

    // Active/pending counts, next upcoming reservation and recent history for the
    // authenticated prosumer. Bearer token attached by AuthInterceptor.
    @GET("dashboard/prosumer")
    suspend fun getProsumerDashboard(): Response<ProsumerDashboardResponse>

    // Pending / approved-future counts and today's schedule for the authenticated
    // operator, already scoped to that operator's assigned nodes (REQ-VIEW AC-6).
    //
    // nodeId is deliberately nullable and defaulted to null: the server derives the
    // scope from the caller's JWT, and an explicit ?nodeId= may only NARROW that scope
    // (BR-11) — passing a node the operator is not assigned to returns E4303/403. The
    // Android client has no way to know a node id (it is absent from both LoginResponse
    // and CurrentUserResponse), so it omits the parameter and lets the server scope.
    @GET("dashboard/operator")
    suspend fun getOperatorDashboard(
        @Query("nodeId") nodeId: String? = null
    ): Response<OperatorDashboardResponse>
}
