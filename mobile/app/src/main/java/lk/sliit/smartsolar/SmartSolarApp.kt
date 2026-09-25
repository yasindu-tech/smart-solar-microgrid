/**
 * File        : SmartSolarApp.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : root
 * Description : Custom Application class. Initialises application-scoped singletons
 *               including ApiClient with the SessionManager token provider.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar

import android.app.Application
import lk.sliit.smartsolar.data.remote.ApiClient
import lk.sliit.smartsolar.util.SessionManager

class SmartSolarApp : Application() {

    override fun onCreate() {
        super.onCreate()

        val sessionManager = SessionManager(this)
        ApiClient.init { sessionManager.getToken() }
    }
}
