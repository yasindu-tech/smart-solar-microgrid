/**
 * File        : CurrentUserResponse.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Response body from GET /auth/me. Returns the identity extracted
 *               from the validated JWT claims on the server side.
 * Author      : <member name - M1>
 */

package lk.sliit.smartsolar.data.remote.dto

data class CurrentUserResponse(
    // UserId or NIC — whatever is in the "sub" claim of the token.
    val subjectId: String,

    // "Backoffice", "Operator", or "Prosumer".
    val role: String,

    // Display name from the "name" claim.
    val displayName: String
)
