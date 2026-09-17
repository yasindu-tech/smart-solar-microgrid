import { createContext, useContext, useEffect, useState } from 'react'
import { getMeApi, logoutApi } from '../api/api'
import { User } from '../api/types'

interface AuthContextType {
  user: User | null
  isLoading: boolean
  login: (token: string, user: User) => void
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    // Attempt to restore session on mount
    const restoreSession = async () => {
      const token = sessionStorage.getItem('token')
      if (!token) {
        setIsLoading(false)
        return
      }

      try {
        const userData = await getMeApi()
        setUser(userData)
      } catch (error) {
        // Token might be expired or invalid
        sessionStorage.removeItem('token')
        setUser(null)
      } finally {
        setIsLoading(false)
      }
    }

    restoreSession()
  }, [])

  const login = (token: string, userData: User) => {
    sessionStorage.setItem('token', token)
    setUser(userData)
  }

  const logout = async () => {
    try {
      if (sessionStorage.getItem('token')) {
        await logoutApi()
      }
    } catch (err) {
      console.warn('Logout API call failed, but clearing local session anyway.')
    } finally {
      sessionStorage.removeItem('token')
      setUser(null)
    }
  }

  return (
    <AuthContext.Provider value={{ user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
