/**
 * File        : ReservationsApi.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote
 * Description : Retrofit service interface for energy reservation endpoints (api-contract.md §6).
 */

package lk.sliit.smartsolar.data.remote

import lk.sliit.smartsolar.data.remote.dto.CreateReservationRequest
import lk.sliit.smartsolar.data.remote.dto.PagedResponse
import lk.sliit.smartsolar.data.remote.dto.ReservationResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path
import retrofit2.http.Query

interface ReservationsApi {

    // Create a new reservation (REQ-RESV §4.1)
    @POST("reservations")
    suspend fun createReservation(
        @Body request: CreateReservationRequest
    ): Response<ReservationResponse>

    // Get signed-in prosumer's own reservations
    @GET("reservations/me")
    suspend fun getMyReservations(
        @Query("status") status: String? = null,
        @Query("page") page: Int = 1,
        @Query("pageSize") pageSize: Int = 10
    ): Response<PagedResponse<ReservationResponse>>

    // Get single reservation by ID
    @GET("reservations/{reservationId}")
    suspend fun getReservationById(
        @Path("reservationId") reservationId: String
    ): Response<ReservationResponse>
}
