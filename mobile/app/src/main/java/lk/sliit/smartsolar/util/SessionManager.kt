/**
 * File        : SessionManager.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : util
 * Description : High-level session helper that wraps SessionDao and adds token
 *               expiry awareness. On app launch, getValidSession() checks the
 *               stored expiry against the device clock and silently clears stale
 *               sessions (REQ-AUTH AC-9). On logout, clearSession() wipes all
 *               four SQLite tables so a second account never sees cached data
 *               from the first (ADR-004, REQ-VIEW EC-7).
 * Author      : <member name - M1>
 */

package lk.sliit.smartsolar.util

import android.content.Context
import lk.sliit.smartsolar.data.local.SessionDao
import lk.sliit.smartsolar.data.local.SessionRow
import lk.sliit.smartsolar.data.local.SmartSolarDatabase
import lk.sliit.smartsolar.data.remote.dto.LoginResponse
import java.time.Instant

class SessionManager(context: Context) {

    private val database = SmartSolarDatabase(context)
    private val sessionDao = SessionDao(database)

    // Persist the login response into the SQLite session table.
    fun saveSession(response: LoginResponse) {
        // Parse the ISO-8601 expiry string to epoch seconds for storage.
        val expiresAtEpoch = try {
            Instant.parse(response.expiresAt).epochSecond
        } catch (_: Exception) {
            // Fallback: if parsing fails, set 8 hours from now (matches JWT TTL).
            Instant.now().plusSeconds(8 * 3600).epochSecond
        }

        sessionDao.save(
            nicOrUserId = response.nicOrUserId,
            role = response.role,
            jwt = response.token,
            expiresAt = expiresAtEpoch,
            fullName = response.displayName
        )
    }

    // Retrieve the session if it exists and has not expired. Expired sessions
    // are deleted silently so the user is sent back to login (AC-9).
    fun getValidSession(): SessionRow? {
        val session = sessionDao.get() ?: return null

        val nowEpoch = Instant.now().epochSecond
        if (session.expiresAt <= nowEpoch) {
            // Token has expired — clear the stale row and return null.
            sessionDao.clear()
            return null
        }

        return session
    }

    // Build the Authorization header value from the stored JWT.
    fun getToken(): String? {
        val session = getValidSession() ?: return null
        return "Bearer ${session.jwt}"
    }

    // Convenience check for whether a valid (non-expired) session exists.
    fun isLoggedIn(): Boolean = getValidSession() != null

    // Wipe all local data on logout. Clears the session row plus all four cache
    // tables so a subsequent login on the same device starts clean (ADR-004).
    fun clearSession() {
        database.clearAll()
    }
}
