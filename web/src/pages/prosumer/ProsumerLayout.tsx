/**
 * File        : ProsumerLayout.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Route shell for the prosumer area - sidebar, header and the
 *               outlet every prosumer screen renders into. Mirrors
 *               OperatorLayout so the three role shells stay visually
 *               identical (ui-design-system.md section 7).
 */

import { Outlet, Link, useNavigate, useLocation } from 'react-router-dom'
import { LayoutDashboard, LogOut } from 'lucide-react'
import { useAuth } from '../../lib/AuthContext'
import UserBadge from '../../components/UserBadge'

export default function ProsumerLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const handleLogout = async () => {
    // Clear the session first so a failed navigation cannot leave the user
    // looking at an authenticated shell.
    await logout()
    navigate('/login')
  }

  // Only the dashboard exists on web today. Booking, map and profile are
  // Android surfaces in REQ-VIEW 2.1, so no nav entry is added for them -
  // a sidebar link to a route that does not exist is worse than its absence.
  const navItems = [{ name: 'Dashboard', path: '/prosumer', icon: LayoutDashboard }]

  return (
    <div className="sg-page flex h-screen overflow-hidden bg-sg-bg">
      <aside className="w-sidebar flex-shrink-0 border-r border-sg-border bg-sg-surface flex flex-col">
        <div className="flex h-header items-center justify-center px-6 border-b border-sg-border">
          <img src="/logo-navbar.png" alt="SunGrid" className="h-10 w-auto" />
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
          <button onClick={handleLogout} className="sg-btn sg-btn-primary-soft w-full">
            <LogOut size={18} className="mr-2" />
            Sign out
          </button>
        </div>
      </aside>

      <main className="flex-1 overflow-y-auto">
        <header className="h-header border-b border-sg-border bg-sg-surface flex items-center justify-between px-8">
          <h1 className="sg-h3">My Energy</h1>
          <UserBadge displayName={user?.displayName} role={user?.role} />
        </header>
        <div className="p-8">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
