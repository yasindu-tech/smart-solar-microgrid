/**
 * File        : LoginResponse.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Response body from POST /auth/login (200 OK). Maps 1:1 to the
 *               backend LoginResponse record. The role field drives which home
 *               screen the app navigates to (REQ-AUTH §4.7).
 * Author      : <member name - M1>
 */

package lk.sliit.smartsolar.data.remote.dto

data class LoginResponse(
    // Signed JWT for subsequent Bearer-authenticated requests.
    val token: String,

    // "Backoffice", "Operator", or "Prosumer" — used to route to the correct home.
    val role: String,

    // Display name for the welcome header.
    val displayName: String,

    // UserId for web users; NIC for prosumers.
    val nicOrUserId: String,

    // UTC expiry so the app can detect stale sessions without calling the API.
    val expiresAt: String
)
