/**
 * File        : RegisterProsumerRequest.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Request body for POST /prosumers/register (REQ-PROS §4.1).
 *               NIC must match ^([0-9]{9}[vVxX]|[0-9]{12})$ (REQ-PROS EC-1).
 *               All validation is enforced server-side; client-side check is
 *               presentational only.
 * Author      : Ricky Perera (M1)
 */

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
