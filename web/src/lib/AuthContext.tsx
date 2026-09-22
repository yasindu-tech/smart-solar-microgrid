import { createContext, useContext, useEffect, useState } from 'react'
import { getMeApi, logoutApi } from '../api/api'
import { session } from '../api/client'
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
      const token = session.getToken()
      if (!token) {
        setIsLoading(false)
        return
      }

      try {
        const userData = await getMeApi()
        setUser(userData)
      } catch {
        // Token might be expired or invalid
        session.clearToken()
        setUser(null)
      } finally {
        setIsLoading(false)
      }
    }

    restoreSession()
  }, [])

  const login = (token: string, userData: User) => {
    session.setToken(token)
    setUser(userData)
  }

  const logout = async () => {
    try {
      if (session.getToken()) {
        await logoutApi()
      }
    } catch {
      console.warn('Logout API call failed, but clearing local session anyway.')
    } finally {
      session.clearToken()
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
