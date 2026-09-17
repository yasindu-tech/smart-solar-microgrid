import axios from 'axios'

// Overridden per environment via .env (see .env.example) - never hard-code a host.
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000/api',
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

export default apiClient
