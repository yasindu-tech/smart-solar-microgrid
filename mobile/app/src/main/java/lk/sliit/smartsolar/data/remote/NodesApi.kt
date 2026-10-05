/**
 * File        : NodesApi.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote
 * Description : Retrofit service interface for microgrid node endpoints (api-contract.md §4).
 *               Fetches active nodes from GET /api/nodes?active=true for the map view (REQ-VIEW §2.5).
 */

package lk.sliit.smartsolar.data.remote

import lk.sliit.smartsolar.data.remote.dto.NodeResponse
import lk.sliit.smartsolar.data.remote.dto.PagedResponse
import retrofit2.Response
import retrofit2.http.GET
import retrofit2.http.Query

interface NodesApi {

    // Fetch active microgrid nodes from GET /api/nodes?active=true&pageSize=100
    @GET("nodes")
    suspend fun getNodes(
        @Query("active") active: Boolean? = true,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 100
    ): Response<PagedResponse<NodeResponse>>
}
