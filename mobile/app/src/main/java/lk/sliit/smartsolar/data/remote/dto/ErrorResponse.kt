/**
 * File        : ErrorResponse.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Maps the standard error envelope returned by every non-2xx API
 *               response. The code field (e.g. E4301) is used to branch on error
 *               type; the message field is safe to display to the user
 *               (see api-contract.md §9).
 * Author      : <member name - M1>
 */

package lk.sliit.smartsolar.data.remote.dto

data class ErrorResponse(
    // Documented error code, e.g. "E4301" for bad credentials.
    val code: String?,

    // Human-readable message safe to show to the user.
    val message: String?,

    // Optional field name that caused the error (e.g. "password").
    val field: String?,

    // Server-side trace identifier for debugging.
    val traceId: String?
)
