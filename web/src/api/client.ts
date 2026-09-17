/**
 * File        : client.ts
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Single place the web app talks to the Web API. Every call goes
 *               through here so the error envelope defined in
 *               docs/architecture/system-overview.md section 5 is handled once.
 *               No business rule is evaluated in this file - it only transports.
 * Author      : <member name - M2>
 */

import { ApiRequestError } from './types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5001/api'

// Issue a request, attaching the bearer token, and translate any non-2xx
// response into an ApiRequestError carrying the documented error code.
export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = sessionStorage.getItem('token')

  const response = await fetch(`${BASE_URL}${path}`, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  })

  if (!response.ok) {
    const envelope = (await response.json().catch(() => ({
      code: 'E5000',
      message: 'A network or parsing error occurred.',
    })))
    throw new ApiRequestError(envelope)
  }

  // Handle empty responses gracefully
  const text = await response.text()
  return text ? JSON.parse(text) : ({} as T)
}
