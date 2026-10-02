/**
 * File        : NodeCacheDao.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.local
 * Description : Data access object for the SQLite `node_cache` table (ADR-004).
 *               Stores active microgrid nodes locally so map pins render instantly on reopen.
 *               Replaced atomically on successful API fetch (BR-20, AC-9).
 *               Read-only for decisions — cache is never authoritative (BR-14).
 */

package lk.sliit.smartsolar.data.local

import android.content.ContentValues
import android.database.sqlite.SQLiteDatabase
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.data.remote.dto.NodeResponse
import java.time.Instant

/**
 * Row representation of the expanded node_cache table.
 */
data class NodeCacheRow(
    val nodeId: String,
    val name: String?,
    val addressLine: String?,
    val latitude: Double?,
    val longitude: Double?,
    val capacityKwh: Double,
    val batterySlotCount: Int,
    val type: String?,
    val isActive: Boolean,
    val syncedAt: Long // epoch seconds
)

class NodeCacheDao(private val db: SmartSolarDatabase) {

    // Atomically replace the node cache within a single SQLite transaction.
    // Clears old entries first so deactivated nodes disappear immediately (BR-20, AC-9).
    suspend fun replaceCache(nodes: List<NodeResponse>) = withContext(Dispatchers.IO) {
        val now = Instant.now().epochSecond
        val sqliteDb = db.writableDatabase
        sqliteDb.beginTransaction()
        try {
            sqliteDb.delete("node_cache", null, null)
            for (node in nodes) {
                val id = node.nodeId ?: continue
                val values = ContentValues().apply {
                    put("node_id", id)
                    put("name", node.name)
                    put("address_line", node.location?.addressLine)
                    put("latitude", node.location?.latitude)
                    put("longitude", node.location?.longitude)
                    put("capacity_kwh", node.capacityKwh)
                    put("battery_slot_count", node.batterySlotCount)
                    put("type", node.type)
                    put("is_active", if (node.isActive) 1 else 0)
                    put("synced_at", now)
                }
                sqliteDb.insertWithOnConflict(
                    "node_cache", null, values,
                    SQLiteDatabase.CONFLICT_REPLACE
                )
            }
            sqliteDb.setTransactionSuccessful()
        } finally {
            sqliteDb.endTransaction()
        }
    }

    // Read all cached nodes for fast local rendering on map launch or offline.
    suspend fun getAll(): List<NodeCacheRow> = withContext(Dispatchers.IO) {
        val cursor = db.readableDatabase.rawQuery(
            """
            SELECT node_id, name, address_line, latitude, longitude,
                   capacity_kwh, battery_slot_count, type, is_active, synced_at
            FROM node_cache
            """.trimIndent(),
            null
        )
        val list = mutableListOf<NodeCacheRow>()
        cursor.use {
            while (it.moveToNext()) {
                list.add(
                    NodeCacheRow(
                        nodeId = it.getString(0),
                        name = it.getString(1),
                        addressLine = it.getString(2),
                        latitude = if (it.isNull(3)) null else it.getDouble(3),
                        longitude = if (it.isNull(4)) null else it.getDouble(4),
                        capacityKwh = if (it.isNull(5)) 0.0 else it.getDouble(5),
                        batterySlotCount = if (it.isNull(6)) 0 else it.getInt(6),
                        type = it.getString(7),
                        isActive = it.getInt(8) == 1,
                        syncedAt = if (it.isNull(9)) 0L else it.getLong(9)
                    )
                )
            }
        }
        list
    }

    // Check if the cached node data is stale (older than 24 hours, REQ-VIEW EC-6).
    suspend fun isStale(): Boolean = withContext(Dispatchers.IO) {
        val cursor = db.readableDatabase.rawQuery(
            "SELECT MIN(synced_at) FROM node_cache", null
        )
        val minSyncedAt = cursor.use {
            if (it.moveToFirst() && !it.isNull(0)) it.getLong(0) else 0L
        }
        if (minSyncedAt == 0L) return@withContext true
        val threshold = Instant.now().epochSecond - (24 * 3600)
        minSyncedAt < threshold
    }

    // Clear all cached nodes. Called as part of clearAll() on logout (REQ-VIEW EC-7).
    suspend fun clear() = withContext(Dispatchers.IO) {
        db.writableDatabase.delete("node_cache", null, null)
    }
}
