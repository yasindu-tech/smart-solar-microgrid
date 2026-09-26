/**
 * File        : ProsumersApi.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote
 * Description : Retrofit service interface for the /prosumers endpoints defined
 *               in api-contract.md §3. Registration is anonymous; authenticated
 *               endpoints receive the Bearer token via AuthInterceptor.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.remote

import lk.sliit.smartsolar.data.remote.dto.ProsumerProfileResponse
import lk.sliit.smartsolar.data.remote.dto.RegisterProsumerRequest
import lk.sliit.smartsolar.data.remote.dto.UpdateProsumerRequest
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.PATCH
import retrofit2.http.PUT
import retrofit2.http.POST
import retrofit2.http.Path

interface ProsumersApi {

    // Register a new prosumer. Public endpoint — no token required (REQ-PROS §4.1).
    @POST("prosumers/register")
    suspend fun register(@Body request: RegisterProsumerRequest): Response<ProsumerProfileResponse>

    // Get the signed-in prosumer's own profile. BR-02 enforced server-side.
    @GET("prosumers/{nic}")
    suspend fun getProfile(
        @Path("nic") nic: String
    ): Response<ProsumerProfileResponse>

    // Update the prosumer's own profile. NIC is in the path — never in the body (BR-12).
    @PUT("prosumers/{nic}")
    suspend fun updateProfile(
        @Path("nic") nic: String,
        @Body request: UpdateProsumerRequest
    ): Response<ProsumerProfileResponse>

    // Request deactivation of the prosumer's own account (REQ-PROS §4.4).
    @PATCH("prosumers/{nic}/deactivate")
    suspend fun deactivate(
        @Path("nic") nic: String
    ): Response<Unit>
}
