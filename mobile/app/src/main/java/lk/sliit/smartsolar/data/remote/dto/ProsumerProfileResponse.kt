/**
 * File        : ProsumerProfileResponse.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Response body for GET /prosumers/{nic} and PUT /prosumers/{nic}.
 *               Maps to the ProsumerResponse DTO in the Web API (api-contract.md §3).
 *               NIC is read-only on the client (BR-12, REQ-PROS §4.3).
 * Author      : Ricky Perera (M1)
 */

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
