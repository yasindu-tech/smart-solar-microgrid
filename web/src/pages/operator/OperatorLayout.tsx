import { Outlet, Link, useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../../lib/AuthContext'
import { LogOut, Calendar, Activity } from 'lucide-react'
import UserBadge from '../../components/UserBadge'

export default function OperatorLayout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  const navItems = [
    { name: 'Dashboard', path: '/operator', icon: Activity },
    { name: 'Reservations', path: '/operator/reservations', icon: Calendar },
  ]

  return (
    <div className="sg-page flex h-screen overflow-hidden bg-sg-bg">
      {/* Sidebar */}
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
          <button
            onClick={handleLogout}
            className="sg-btn sg-btn-primary-soft w-full"
          >
            <LogOut size={18} className="mr-2" />
            Sign out
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 overflow-y-auto">
        <header className="h-header border-b border-sg-border bg-sg-surface flex items-center justify-between px-8">
          <h1 className="sg-h3">Grid Operations</h1>
          <UserBadge displayName={user?.displayName} role={user?.role} />
        </header>
        <div className="p-8">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
