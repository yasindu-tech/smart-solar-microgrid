/**
 * File        : ApiClient.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote
 * Description : Singleton Retrofit client configured with the API base URL from
 *               BuildConfig and Gson for JSON serialisation. Provides lazily
 *               initialised service instances for each API group (Auth, Prosumers).
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.remote

import lk.sliit.smartsolar.BuildConfig
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

object ApiClient {

    // Build Retrofit once using the base URL configured in build.gradle.kts.
    private val retrofit: Retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
    }

    // Lazily create the auth API service.
    val authApi: AuthApi by lazy {
        retrofit.create(AuthApi::class.java)
    }

    // Lazily create the prosumers API service (register, profile, deactivate).
    val prosumersApi: ProsumersApi by lazy {
        retrofit.create(ProsumersApi::class.java)
    }
}
