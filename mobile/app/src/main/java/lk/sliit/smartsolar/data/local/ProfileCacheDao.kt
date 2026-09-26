/**
 * File        : ProfileCacheDao.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.local
 * Description : Data access object for the SQLite `profile_cache` table (ADR-004).
 *               Stores the signed-in prosumer's own profile for display-only purposes.
 *               The cache is read-only for decisions (BR-14) — all mutations go
 *               through the API. Cleared on logout (ADR-004, REQ-VIEW EC-7).
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.local

import android.content.ContentValues
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.time.Instant

/**
 * Thin row representation of the profile_cache table.
 * Maps to the columns defined in SmartSolarDatabase.onCreate().
 */
data class ProfileCacheRow(
    val nic: String,
    val fullName: String?,
    val email: String?,
    val phone: String?,
    val address: String?,
    val solarCapacityKw: Double?,
    val status: String?,
    val syncedAt: Long   // epoch seconds — used to show the offline banner (REQ-VIEW EC-6)
)

class ProfileCacheDao(private val db: SmartSolarDatabase) {

    // Write or replace the signed-in prosumer's profile row on IO dispatcher.
    suspend fun save(row: ProfileCacheRow) = withContext(Dispatchers.IO) {
        val values = ContentValues().apply {
            put("nic", row.nic)
            put("full_name", row.fullName)
            put("email", row.email)
            put("phone", row.phone)
            put("address", row.address)
            put("solar_capacity_kw", row.solarCapacityKw)
            put("status", row.status)
            put("synced_at", row.syncedAt)
        }
        db.writableDatabase.insertWithOnConflict(
            "profile_cache", null, values,
            android.database.sqlite.SQLiteDatabase.CONFLICT_REPLACE
        )
    }

    // Read the cached profile for the given NIC on IO dispatcher, or null if not cached.
    suspend fun get(nic: String): ProfileCacheRow? = withContext(Dispatchers.IO) {
        val cursor = db.readableDatabase.rawQuery(
            "SELECT nic, full_name, email, phone, address, solar_capacity_kw, status, synced_at FROM profile_cache WHERE nic = ?",
            arrayOf(nic)
        )
        cursor.use {
            if (it.moveToFirst()) {
                ProfileCacheRow(
                    nic = it.getString(0),
                    fullName = it.getString(1),
                    email = it.getString(2),
                    phone = it.getString(3),
                    address = it.getString(4),
                    solarCapacityKw = if (it.isNull(5)) null else it.getDouble(5),
                    status = it.getString(6),
                    syncedAt = it.getLong(7)
                )
            } else {
                null
            }
        }
    }

    // Check if the cached data is stale on IO dispatcher (older than 24 hours, per REQ-VIEW EC-6).
    suspend fun isStale(nic: String): Boolean = withContext(Dispatchers.IO) {
        val cached = get(nic) ?: return@withContext true
        val threshold = Instant.now().epochSecond - (24 * 3600)
        cached.syncedAt < threshold
    }

    // Clear the cache for the given NIC on IO dispatcher. Called as part of clearAll() on logout.
    suspend fun clear(nic: String) = withContext(Dispatchers.IO) {
        db.writableDatabase.delete("profile_cache", "nic = ?", arrayOf(nic))
    }
}
