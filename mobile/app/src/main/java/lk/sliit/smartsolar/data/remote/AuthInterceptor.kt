/**
 * File        : AuthInterceptor.kt
 * Project     : Smart Solar Microgrid Trading System - Android client
 * Module      : data.remote
 * Description : OkHttp Interceptor that automatically attaches the Bearer JWT
 *               token from SessionManager to every outgoing HTTP request, unless
 *               already present. Eliminates boilerplate @Header annotations across
 *               all Retrofit interfaces.
 * Author      : Ricky Perera (M1)
 */

package lk.sliit.smartsolar.data.remote

import okhttp3.Interceptor
import okhttp3.Response

class AuthInterceptor(
    private val tokenProvider: () -> String?
) : Interceptor {

    override fun intercept(chain: Interceptor.Chain): Response {
        val original = chain.request()

        // If the request already specifies an Authorization header, don't overwrite it.
        if (original.header("Authorization") != null) {
            return chain.proceed(original)
        }

        val token = tokenProvider()
        val request = if (!token.isNullOrBlank()) {
            original.newBuilder()
                .header("Authorization", token)
                .build()
        } else {
            original
        }

        return chain.proceed(request)
    }
}
