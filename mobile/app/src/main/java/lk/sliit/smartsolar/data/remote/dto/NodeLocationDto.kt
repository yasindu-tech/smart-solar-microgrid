/**
 * File        : NodeLocationDto.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Location DTO for a microgrid node (lat/lng/addressLine).
 *               Latitude and longitude are nullable to safely handle and skip
 *               malformed coordinate data (REQ-VIEW EC-3).
 */

package lk.sliit.smartsolar.data.remote.dto

data class NodeLocationDto(
    val latitude: Double? = null,
    val longitude: Double? = null,
    val addressLine: String? = null
)
