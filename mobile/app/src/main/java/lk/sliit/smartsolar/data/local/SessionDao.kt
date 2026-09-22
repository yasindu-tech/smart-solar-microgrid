/**
 * File        : SessionDao.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.local
 * Description : Data access object for the SQLite `session` table defined in
 *               ADR-004. The table holds exactly one row (id = 1) containing the
 *               signed-in user's JWT, role, and display name so the app survives
 *               a restart without requiring re-authentication (REQ-AUTH AC-8).
 * Author      : <member name - M1>
 */

package lk.sliit.smartsolar.data.local

import android.content.ContentValues
import android.database.Cursor

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

    // Persist or replace the single session row after a successful login.
    fun save(nicOrUserId: String, role: String, jwt: String, expiresAt: Long, fullName: String?) {
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

    // Read the current session, or null if no user is logged in.
    fun get(): SessionRow? {
        val cursor: Cursor = db.readableDatabase.rawQuery(
            "SELECT nic_or_user_id, role, jwt, expires_at, full_name FROM session WHERE id = 1",
            null
        )
        // Map the single row to a SessionRow, or return null if the table is empty.
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

    // Remove the session row. Called on logout and on expired-session detection.
    fun clear() {
        db.writableDatabase.delete("session", null, null)
    }
}
