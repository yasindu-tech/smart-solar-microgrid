/**
 * File        : SessionDao.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.local
 * Description : Data access object for the SQLite `session` table defined in
 *               ADR-004. Operations run on Dispatchers.IO to prevent main-thread
 *               disk I/O. Holds exactly one row (id = 1) containing the signed-in
 *               user's JWT, role, and display name so the app survives a restart
 *               without requiring re-authentication (REQ-AUTH AC-8).
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.local

import android.content.ContentValues
import android.database.Cursor
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

/**
 * Thin row representation of the session table. Not a domain model — just the
 * six columns from SQLite, typed for Kotlin convenience.
 */
data class SessionRow(
    val nicOrUserId: String,
    val role: String,
    val jwt: String,
    val expiresAt: Long,
    val fullName: String?
)

class SessionDao(private val db: SmartSolarDatabase) {

    // Persist or replace the single session row after a successful login on IO dispatcher.
    suspend fun save(nicOrUserId: String, role: String, jwt: String, expiresAt: Long, fullName: String?) =
        withContext(Dispatchers.IO) {
            val values = ContentValues().apply {
                put("id", 1)
                put("nic_or_user_id", nicOrUserId)
                put("role", role)
                put("jwt", jwt)
                put("expires_at", expiresAt)
                put("full_name", fullName)
            }
            // INSERT OR REPLACE — the CHECK (id = 1) constraint ensures only one row.
            db.writableDatabase.insertWithOnConflict(
                "session", null, values,
                android.database.sqlite.SQLiteDatabase.CONFLICT_REPLACE
            )
        }

    // Read the current session, or null if no user is logged in, on IO dispatcher.
    suspend fun get(): SessionRow? = withContext(Dispatchers.IO) {
        getSync()
    }

    // Synchronous read for non-coroutine contexts (e.g. cold-start AuthInterceptor).
    fun getSync(): SessionRow? {
        val cursor: Cursor = db.readableDatabase.rawQuery(
            "SELECT nic_or_user_id, role, jwt, expires_at, full_name FROM session WHERE id = 1",
            null
        )
        return cursor.use {
            if (it.moveToFirst()) {
                SessionRow(
                    nicOrUserId = it.getString(0),
                    role = it.getString(1),
                    jwt = it.getString(2),
                    expiresAt = it.getLong(3),
                    fullName = it.getString(4)
                )
            } else {
                null
            }
        }
    }

    // Remove the session row on IO dispatcher.
    suspend fun clear() = withContext(Dispatchers.IO) {
        clearSync()
    }

    // Synchronous clear for database resets.
    fun clearSync() {
        db.writableDatabase.delete("session", null, null)
    }
}
