/**
 * File        : ApiClient.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote
 * Description : Singleton Retrofit client configured with the API base URL from
 *               BuildConfig and Gson for JSON serialisation. Provides lazily
 *               initialised service instances for each API group (Auth, Prosumers,
 *               Dashboard).
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.remote

import lk.sliit.smartsolar.BuildConfig
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

object ApiClient {

    private var tokenProvider: (() -> String?)? = null

    // Called during app initialisation to supply the token provider.
    fun init(provider: () -> String?) {
        tokenProvider = provider
    }

    // OkHttpClient configured with authentication and logging interceptors.
    private val okHttpClient: OkHttpClient by lazy {
        val loggingInterceptor = HttpLoggingInterceptor().apply {
            level = if (BuildConfig.DEBUG) {
                HttpLoggingInterceptor.Level.BODY
            } else {
                HttpLoggingInterceptor.Level.NONE
            }
        }

        OkHttpClient.Builder()
            .addInterceptor(AuthInterceptor { tokenProvider?.invoke() })
            .addInterceptor(loggingInterceptor)
            .connectTimeout(15, TimeUnit.SECONDS)
            .readTimeout(15, TimeUnit.SECONDS)
            .writeTimeout(15, TimeUnit.SECONDS)
            .build()
    }

    // Build Retrofit once using the base URL and OkHttpClient.
    private val retrofit: Retrofit by lazy {
        Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .client(okHttpClient)
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

    // Lazily create the dashboard API service (prosumer dashboard metrics).
    val dashboardApi: DashboardApi by lazy {
        retrofit.create(DashboardApi::class.java)
    }

    // Lazily create the nodes API service (nearby microgrid nodes for map).
    val nodesApi: NodesApi by lazy {
        retrofit.create(NodesApi::class.java)
    }
}
