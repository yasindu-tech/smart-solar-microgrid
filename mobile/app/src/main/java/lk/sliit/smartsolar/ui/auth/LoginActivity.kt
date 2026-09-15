/**
 * File        : LoginActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.auth
 * Description : Entry screen. Prosumers sign in with NIC, operators with email.
 *               The role claim in the returned token decides which home screen
 *               is shown (REQ-AUTH, AC-3 and AC-4). No rule is evaluated here.
 * Author      : <member name - M1>
 */

package lk.sliit.smartsolar.ui.auth

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import lk.sliit.smartsolar.R

class LoginActivity : AppCompatActivity() {

    // Inflate the login screen. Session restore from SQLite happens here once
    // SessionDao is implemented - see REQ-AUTH AC-8.
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContentView(R.layout.activity_login)
    }
}
