/**
 * File        : LoginRequest.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Request body for POST /auth/login. The identifier field accepts
 *               either an email (web users) or a NIC (prosumers) — the API
 *               determines which collection to query (REQ-AUTH §4.1–2).
 * Author      : <member name - M1>
 */

package lk.sliit.smartsolar.data.remote.dto

data class LoginRequest(
    // Email for Backoffice/Operator accounts; NIC for Prosumer accounts.
    val identifier: String,
    val password: String
)
