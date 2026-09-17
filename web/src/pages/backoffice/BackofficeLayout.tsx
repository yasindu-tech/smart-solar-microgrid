/**
 * File        : BackofficeLayout.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Layout shell and navigation for the Backoffice administration view.
 * Author      : <member name - M2>
 */

import React from 'react'
import { Outlet, Link, useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../../lib/AuthContext'
import { LogOut, Users, Battery, CheckCircle } from 'lucide-react'

export default function BackofficeLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  const navItems = [
    { name: 'Dashboard', path: '/backoffice', icon: Battery },
    { name: 'Users', path: '/backoffice/users', icon: Users },
    { name: 'Approvals', path: '/backoffice/approvals', icon: CheckCircle },
  ]

  return (
    <div className="sg-page flex h-screen overflow-hidden bg-sg-bg">
      {/* Sidebar */}
      <aside className="w-sidebar flex-shrink-0 border-r border-sg-border bg-sg-surface flex flex-col">
        <div className="flex h-header items-center px-6 border-b border-sg-border">
          <img src="/logo-navbar.png" alt="SunGrid" className="h-8 w-auto" />
        </div>
        
        <div className="p-4 flex-1 overflow-y-auto">
          <nav className="sg-stack-sm">
            {navItems.map((item) => {
              const isActive = location.pathname === item.path
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  className={`flex items-center gap-3 px-3 py-2 rounded-md transition-colors ${
                    isActive 
                      ? 'bg-sg-primary-soft text-sg-primary font-medium' 
                      : 'text-sg-text-secondary hover:bg-sg-surface-muted hover:text-sg-text'
                  }`}
                >
                  <item.icon size={20} />
                  {item.name}
                </Link>
              )
            })}
          </nav>
        </div>

        <div className="p-4 border-t border-sg-border">
          <div className="mb-4">
            <p className="text-sm font-medium text-sg-text truncate">{user?.displayName}</p>
            <p className="text-xs text-sg-text-muted">{user?.role}</p>
          </div>
          <button 
            onClick={handleLogout}
            className="sg-btn sg-btn-ghost w-full justify-start text-sg-danger-text hover:bg-sg-danger-soft"
          >
            <LogOut size={18} className="mr-2" />
            Sign out
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-y-auto">
        <header className="h-header border-b border-sg-border bg-sg-surface flex items-center px-8">
          <h1 className="sg-h3">Backoffice Administration</h1>
        </header>
        <div className="p-8">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
