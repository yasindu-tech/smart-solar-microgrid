import { useState } from 'react'
import { loginApi } from './api'
import { ApiRequestError, LoginCredentials, LoginResponse } from './types'

export function useLogin() {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState('')

  const loginAction = async (credentials: LoginCredentials): Promise<LoginResponse | null> => {
    setIsLoading(true)
    setError('')

    try {
      const response = await loginApi(credentials)

      // The frontend respects the C2/C3 client split requirement (REQ-AUTH).
      // Prosumers must use the Android app.
      if (response.role === 'Prosumer') {
        setError('Prosumers must use the Android application to log in.')
        return null
      }

      return response
    } catch (err: unknown) {
      if (err instanceof ApiRequestError) {
        setError(err.envelope.message || 'An error occurred during sign in.')
      } else {
        setError('An unexpected error occurred.')
      }
      return null
    } finally {
      setIsLoading(false)
    }
  }

  return { loginAction, isLoading, error }
}
