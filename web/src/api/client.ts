import axios from 'axios'
import { ApiRequestError, ApiError } from './types'

// Overridden per environment via .env (see .env.example) - never hard-code a host.
const apiClient = axios.create({
  // Using 5001 here to match the backend HTTPS configuration
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5001/api',
})

const TOKEN_STORAGE_KEY = 'token'

// The session token, and nothing else, lives here - the one place that reads
// or writes it, so every other file treats the session as opaque.
export const session = {
  getToken: (): string | null => sessionStorage.getItem(TOKEN_STORAGE_KEY),
  setToken: (token: string): void => sessionStorage.setItem(TOKEN_STORAGE_KEY, token),
  clearToken: (): void => sessionStorage.removeItem(TOKEN_STORAGE_KEY),
}

apiClient.interceptors.request.use((config) => {
  const token = session.getToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Translate axios errors into our domain ApiRequestError so the rest of the app 
// can cleanly catch them.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response && error.response.data) {
      const envelope = error.response.data as ApiError
      return Promise.reject(new ApiRequestError(envelope))
    }
    return Promise.reject(
      new ApiRequestError({
        code: 'E5000',
        message: 'A network or parsing error occurred.',
      })
    )
  }
)

export default apiClient
