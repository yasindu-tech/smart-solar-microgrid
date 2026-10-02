/**
 * File        : PagedResponse.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote.dto
 * Description : Generic envelope for paged API responses (api-contract.md).
 */

package lk.sliit.smartsolar.data.remote.dto

data class PagedResponse<T>(
    val items: List<T> = emptyList(),
    val total: Int = 0,
    val page: Int = 1,
    val pageSize: Int = 100
)
