package lk.sliit.smartsolar.data.remote.dto

data class ProsumerProfileResponse(
    val nic: String,
    val fullName: String,
    val email: String,
    val phone: String,
    val address: String,
    val solarCapacityKw: Double,
    val status: String
)
