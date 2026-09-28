/**
 * File        : WelcomeActivity.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : ui.auth
 * Description : Branded landing screen and launcher activity. Shown once per cold start,
 *               after the native splash flash and before LoginActivity. Purely
 *               presentational — no session check, no SQLite read, no network call.
 *               The AC-8/AC-9 session-restore logic stays entirely in
 *               LoginActivity.onCreate and still runs every time that screen is reached,
 *               whether via this screen's "Get Started" button or a direct relaunch.
 */

package lk.sliit.smartsolar.ui.auth

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.core.splashscreen.SplashScreen.Companion.installSplashScreen
import lk.sliit.smartsolar.databinding.ActivityWelcomeBinding

class WelcomeActivity : AppCompatActivity() {

    private lateinit var binding: ActivityWelcomeBinding

    // Show the native splash flash, then inflate the welcome screen and wire the CTA.
    override fun onCreate(savedInstanceState: Bundle?) {
        installSplashScreen()
        super.onCreate(savedInstanceState)
        binding = ActivityWelcomeBinding.inflate(layoutInflater)
        setContentView(binding.root)

        // "Get Started" hands off to LoginActivity, which owns all session/auth logic.
        binding.btnGetStarted.setOnClickListener {
            startActivity(Intent(this, LoginActivity::class.java))
            finish()
        }
    }
}
