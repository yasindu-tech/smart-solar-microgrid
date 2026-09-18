/**
 * File        : App.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Root component and route shell. Role routing lands Backoffice
 *               and Grid Operator users on different dashboards (REQ-AUTH).
 * Author      : <member name - M2>
 */

import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './lib/AuthContext'
import ProtectedRoute from './components/ProtectedRoute'
import Home from './pages/Home'
import Login from './pages/Login'
import BackofficeLayout from './pages/backoffice/BackofficeLayout'
import Dashboard from './pages/backoffice/Dashboard'
import NodeManagement from './pages/backoffice/NodeManagement'
import SlotManagement from './pages/backoffice/SlotManagement'
import OperatorLayout from './pages/operator/OperatorLayout'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/login" element={<Login />} />

          <Route element={<ProtectedRoute allowedRoles={['Backoffice']} />}>
            <Route path="/backoffice" element={<BackofficeLayout />}>
              <Route index element={<Dashboard />} />
              <Route path="nodes" element={<NodeManagement />} />
              <Route path="nodes/:nodeId/slots" element={<SlotManagement />} />
              <Route path="users" element={<div>Users Management Coming Soon</div>} />
              <Route path="approvals" element={<div>Pending Approvals Coming Soon</div>} />
            </Route>
          </Route>

          <Route element={<ProtectedRoute allowedRoles={['Operator']} />}>
            <Route path="/operator" element={<OperatorLayout />}>
              <Route index element={<div>Operator Dashboard Coming Soon</div>} />
              <Route path="reservations" element={<div>Reservations Coming Soon</div>} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
