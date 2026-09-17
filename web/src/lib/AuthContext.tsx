/**
 * File        : AuthContext.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Manages the authentication state, session persistence, and logout flow.
 * Author      : <member name - M2>
 */

import React, { createContext, useContext, useEffect, useState } from 'react'
import { request } from '../api/client'

export interface User {
  subjectId: string
  role: string
  displayName: string
}

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
        const principal = await request<User>('/auth/me')
        setUser(principal)
      } catch (error) {
        // Token is invalid or expired
        sessionStorage.removeItem('token')
        setUser(null)
      } finally {
        setIsLoading(false)
      }
    }

    restoreSession()
  }, [])

  const login = (token: string, user: User) => {
    sessionStorage.setItem('token', token)
    setUser(user)
  }

  const logout = async () => {
    try {
      await request('/auth/logout', { method: 'POST' })
    } catch (error) {
      // Even if backend call fails (e.g. network error), we still clear local state
      console.error('Logout API call failed:', error)
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
