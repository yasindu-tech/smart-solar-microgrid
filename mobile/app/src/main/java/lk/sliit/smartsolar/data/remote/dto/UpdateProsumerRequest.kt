/**
 * File        : UpdateProsumerRequest.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Request body for PUT /prosumers/{nic} (REQ-PROS §4.3).
 *               NIC is excluded — it is immutable per BR-12 and ADR-002.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.remote.dto

data class UpdateProsumerRequest(
    val fullName: String,
    val email: String,
    val phone: String,
    val address: String,
    val solarCapacityKw: Double
)
