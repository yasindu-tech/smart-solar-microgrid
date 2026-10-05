/**
 * File        : SessionManager.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : util
 * Description : High-level session helper that wraps SessionDao and adds token
 *               expiry awareness. All database operations run on Dispatchers.IO.
 *               Uses an in-memory cache for instantaneous, non-blocking token
 *               retrieval by AuthInterceptor. On logout, clearSession() wipes all
 *               four SQLite tables (ADR-004, REQ-VIEW EC-7).
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.util

import android.content.Context
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import lk.sliit.smartsolar.data.local.SessionDao
import lk.sliit.smartsolar.data.local.SessionRow
import lk.sliit.smartsolar.data.local.SmartSolarDatabase
import lk.sliit.smartsolar.data.remote.dto.LoginResponse
import java.time.Instant

class SessionManager(context: Context) {

    private val database = SmartSolarDatabase.getInstance(context)
    private val sessionDao = SessionDao(database)

    companion object {
        // In-memory cache for fast, non-blocking token lookups by AuthInterceptor.
        @Volatile
        private var cachedSession: SessionRow? = null
    }

    // Persist the login response into SQLite session table on IO dispatcher.
    suspend fun saveSession(response: LoginResponse) {
        val expiresAtEpoch = try {
            Instant.parse(response.expiresAt).epochSecond
        } catch (_: Exception) {
            Instant.now().plusSeconds(8 * 3600).epochSecond
        }

        val row = SessionRow(
            nicOrUserId = response.nicOrUserId,
            role = response.role,
            jwt = response.token,
            expiresAt = expiresAtEpoch,
            fullName = response.displayName
        )
        cachedSession = row

        sessionDao.save(
            nicOrUserId = row.nicOrUserId,
            role = row.role,
            jwt = row.jwt,
            expiresAt = row.expiresAt,
            fullName = row.fullName
        )
    }

    // Retrieve the session if it exists and has not expired on IO dispatcher.
    suspend fun getValidSession(): SessionRow? {
        val session = cachedSession ?: sessionDao.get()?.also { cachedSession = it } ?: return null

        val nowEpoch = Instant.now().epochSecond
        if (session.expiresAt <= nowEpoch) {
            cachedSession = null
            sessionDao.clear()
            return null
        }

        return session
    }

    // Fast synchronous token lookup for AuthInterceptor.
    fun getToken(): String? {
        val session = cachedSession ?: sessionDao.getSync()?.also { cachedSession = it } ?: return null

        val nowEpoch = Instant.now().epochSecond
        if (session.expiresAt <= nowEpoch) {
            cachedSession = null
            return null
        }

        return "Bearer ${session.jwt}"
    }

    // Convenience check for whether a valid (non-expired) session exists.
    suspend fun isLoggedIn(): Boolean = getValidSession() != null

    // Wipe all local data on logout on IO dispatcher.
    suspend fun clearSession() = withContext(Dispatchers.IO) {
        cachedSession = null
        database.clearAll()
    }
}
