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
