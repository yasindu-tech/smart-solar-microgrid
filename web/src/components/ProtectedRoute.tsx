import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '../lib/AuthContext'
import SolarLoader from './SolarLoader/SolarLoader'

export default function ProtectedRoute({ allowedRoles }: { allowedRoles: string[] }) {
  const { user, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-sg-background">
        <SolarLoader size="md" showLabel={false} className="text-sg-primary" />
      </div>
    )
  }

  if (!user) {
    return <Navigate to="/login" replace />
  }

  if (!allowedRoles.includes(user.role)) {
    // If authenticated but wrong role, send them back to root which handles role-based routing or login
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
