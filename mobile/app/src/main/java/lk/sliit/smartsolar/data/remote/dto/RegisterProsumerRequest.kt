package lk.sliit.smartsolar.data.remote.dto

data class RegisterProsumerRequest(
    val nic: String,
    val fullName: String,
    val email: String,
    val phone: String,
    val address: String,
    val password: String,
    val solarCapacityKw: Double
)
