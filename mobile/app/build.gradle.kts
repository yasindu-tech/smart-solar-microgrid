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

        // Base URL of the Web API. 10.0.2.2 is the emulator's route to the host.
        buildConfigField("String", "API_BASE_URL", "\"http://10.0.2.2:5000/api/\"")
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

    testImplementation("junit:junit:4.13.2")
}
