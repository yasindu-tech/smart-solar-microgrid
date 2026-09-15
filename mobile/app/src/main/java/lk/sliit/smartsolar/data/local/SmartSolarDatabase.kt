/**
 * File        : SmartSolarDatabase.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.local
 * Description : SQLite helper. Holds ONLY the four tables agreed in
 *               docs/decisions/ADR-004-sqlite-scope-on-android.md: session,
 *               profile_cache, node_cache, reservation_cache. The cache may be
 *               rendered from, but never decided from (BR-14).
 * Author      : <member name - M4>
 */

package lk.sliit.smartsolar.data.local

import android.content.Context
import android.database.sqlite.SQLiteDatabase
import android.database.sqlite.SQLiteOpenHelper

class SmartSolarDatabase(context: Context) :
    SQLiteOpenHelper(context, DATABASE_NAME, null, DATABASE_VERSION) {

    // Create the four local tables on first launch.
    override fun onCreate(db: SQLiteDatabase) {
        db.execSQL(
            """
            CREATE TABLE session (
                id             INTEGER PRIMARY KEY CHECK (id = 1),
                nic_or_user_id TEXT NOT NULL,
                role           TEXT NOT NULL,
                jwt            TEXT NOT NULL,
                expires_at     INTEGER NOT NULL,
                full_name      TEXT
            )
            """.trimIndent()
        )

        db.execSQL(
            """
            CREATE TABLE profile_cache (
                nic       TEXT PRIMARY KEY,
                full_name TEXT,
                email     TEXT,
                phone     TEXT,
                address   TEXT,
                status    TEXT,
                synced_at INTEGER
            )
            """.trimIndent()
        )

        db.execSQL(
            """
            CREATE TABLE node_cache (
                node_id   TEXT PRIMARY KEY,
                name      TEXT,
                latitude  REAL,
                longitude REAL,
                is_active INTEGER,
                synced_at INTEGER
            )
            """.trimIndent()
        )

        db.execSQL(
            """
            CREATE TABLE reservation_cache (
                reservation_id  TEXT PRIMARY KEY,
                node_id         TEXT,
                slot_start_time INTEGER,
                status          TEXT,
                synced_at       INTEGER
            )
            """.trimIndent()
        )
    }

    // The schema is a cache, so an upgrade simply rebuilds it from the API.
    override fun onUpgrade(db: SQLiteDatabase, oldVersion: Int, newVersion: Int) {
        db.execSQL("DROP TABLE IF EXISTS session")
        db.execSQL("DROP TABLE IF EXISTS profile_cache")
        db.execSQL("DROP TABLE IF EXISTS node_cache")
        db.execSQL("DROP TABLE IF EXISTS reservation_cache")
        onCreate(db)
    }

    // Clear every local table. Called on logout so a second account on the same
    // device never sees the first account's cached data (REQ-VIEW EC-7).
    fun clearAll() {
        writableDatabase.apply {
            delete("session", null, null)
            delete("profile_cache", null, null)
            delete("node_cache", null, null)
            delete("reservation_cache", null, null)
        }
    }

    companion object {
        private const val DATABASE_NAME = "smartsolar.db"
        private const val DATABASE_VERSION = 1
    }
}
