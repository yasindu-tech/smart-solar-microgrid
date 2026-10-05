/**
 * File        : AuthApi.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote
 * Description : Retrofit service interface for the /auth endpoints defined in
 *               api-contract.md §1. Login is anonymous; logout and me automatically
 *               receive the Bearer token via AuthInterceptor.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.remote

import lk.sliit.smartsolar.data.remote.dto.CurrentUserResponse
import lk.sliit.smartsolar.data.remote.dto.LoginRequest
import lk.sliit.smartsolar.data.remote.dto.LoginResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST

interface AuthApi {

    // Authenticate with identifier (NIC or email) and password. No token required.
    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>

    // Invalidate the current session. Bearer token attached by AuthInterceptor.
    @POST("auth/logout")
    suspend fun logout(): Response<Unit>

    // Retrieve the authenticated user's profile from JWT claims. Bearer token attached by AuthInterceptor.
    @GET("auth/me")
    suspend fun me(): Response<CurrentUserResponse>
}
