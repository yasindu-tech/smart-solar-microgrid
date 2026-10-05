/**
 * File        : SlotsApi.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote
 * Description : Retrofit service interface for energy booking slot endpoints (api-contract.md §5).
 */

package lk.sliit.smartsolar.data.remote

import lk.sliit.smartsolar.data.remote.dto.SlotResponse
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Path
import retrofit2.http.Query

interface SlotsApi {

    // Fetch available slots for a node (GET /api/nodes/{nodeId}/slots?from=&to=)
    @GET("nodes/{nodeId}/slots")
    suspend fun getSlotsForNode(
        @Path("nodeId") nodeId: String,
        @Query("from") from: String? = null,
        @Query("to") to: String? = null
    ): Response<List<SlotResponse>>
}
