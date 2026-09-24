package lk.sliit.smartsolar.data.remote.dto

data class CurrentUserResponse(
    // UserId or NIC — whatever is in the "sub" claim of the token.
    val subjectId: String,

    // "Backoffice", "Operator", or "Prosumer".
    val role: String,

    // Display name from the "name" claim.
    val displayName: String
)
