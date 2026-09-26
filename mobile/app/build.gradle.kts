import java.io.FileInputStream
import java.util.Properties

// App module. Pure native Android with Kotlin - no cross-platform framework is
// permitted (see docs/project-config.md section 2). SQLite is used for local
// persistence only, per docs/decisions/ADR-004-sqlite-scope-on-android.md.
plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "lk.sliit.smartsolar"
    compileSdk = 34

    defaultConfig {
        applicationId = "lk.sliit.smartsolar"
        minSdk = 24
        targetSdk = 34
        versionCode = 1
        versionName = "0.1.0"

        // Base URL of the Web API. Read from local.properties (not committed) so
        // each developer and CI can point at their own instance. Default is the
        // Android emulator loopback to the host machine on port 5001.
        val localProperties = Properties().apply {
            val localFile = rootProject.file("local.properties")
            if (localFile.exists()) {
                FileInputStream(localFile).use { load(it) }
            }
        }
        val apiUrl = localProperties.getProperty("API_BASE_URL")
            ?: (project.findProperty("API_BASE_URL") as String?)
            ?: "http://10.0.2.2:5001/api/"
        buildConfigField("String", "API_BASE_URL", "\"$apiUrl\"")
    }

    buildFeatures {
        buildConfig = true
        viewBinding = true
    }

    buildTypes {
        release {
            isMinifyEnabled = false
        }
    }

    compileOptions {
        // Enable desugaring so java.time.Instant works on API < 26 (minSdk = 24).
        isCoreLibraryDesugaringEnabled = true
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }
}

dependencies {
    implementation("androidx.core:core-ktx:1.13.1")
    implementation("androidx.appcompat:appcompat:1.7.0")
    implementation("com.google.android.material:material:1.12.0")
    implementation("androidx.constraintlayout:constraintlayout:2.1.4")

    // Networking to the Web API - the only source of business decisions.
    implementation("com.squareup.retrofit2:retrofit:2.11.0")
    implementation("com.squareup.retrofit2:converter-gson:2.11.0")
    implementation("com.squareup.okhttp3:logging-interceptor:4.12.0")

    // Coroutines for async API calls on the IO dispatcher.
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.8.1")

    // Lifecycle — provides lifecycleScope for structured coroutine launching.
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.8.4")

    // Core library desugaring — java.time.Instant support on API < 26.
    coreLibraryDesugaring("com.android.tools:desugar_jdk_libs:2.0.4")

    testImplementation("junit:junit:4.13.2")
}
