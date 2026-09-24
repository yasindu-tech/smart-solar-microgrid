package lk.sliit.smartsolar.data.remote.dto

data class LoginRequest(
    // Email for Backoffice/Operator accounts; NIC for Prosumer accounts.
    val identifier: String,
    val password: String
)
