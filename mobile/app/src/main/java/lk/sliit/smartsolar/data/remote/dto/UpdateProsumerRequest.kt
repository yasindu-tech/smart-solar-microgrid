package lk.sliit.smartsolar.data.remote.dto

data class UpdateProsumerRequest(
    val fullName: String,
    val email: String,
    val phone: String,
    val address: String,
    val solarCapacityKw: Double
)
