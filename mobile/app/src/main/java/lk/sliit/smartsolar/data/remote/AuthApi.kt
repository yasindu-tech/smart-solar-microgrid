package lk.sliit.smartsolar.data.remote

import lk.sliit.smartsolar.data.remote.dto.CurrentUserResponse
import lk.sliit.smartsolar.data.remote.dto.LoginRequest
import lk.sliit.smartsolar.data.remote.dto.LoginResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST

interface AuthApi {

    // Authenticate with identifier (NIC or email) and password. No token required.
    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>

    // Invalidate the current session. Requires a valid Bearer token.
    @POST("auth/logout")
    suspend fun logout(@Header("Authorization") bearerToken: String): Response<Unit>

    // Retrieve the authenticated user's profile from JWT claims. Requires a valid Bearer token.
    @GET("auth/me")
    suspend fun me(@Header("Authorization") bearerToken: String): Response<CurrentUserResponse>
}
