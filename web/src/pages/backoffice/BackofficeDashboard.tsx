import { useEffect, useState } from 'react'
import { Users, UserCheck, Zap, Battery } from 'lucide-react'
import { fetchBackofficeDashboard } from '../../api/dashboardApi'
import type { BackofficeDashboard as DashboardData } from '../../types/DashboardTypes'
import { getErrorMessage } from '../../api/errors'

export default function BackofficeDashboard() {
  const [data, setData] = useState<DashboardData | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    const load = async () => {
      try {
        setIsLoading(true)
        const dashboard = await fetchBackofficeDashboard()
        if (!cancelled) setData(dashboard)
      } catch (err) {
        if (!cancelled) setError(getErrorMessage(err))
      } finally {
        if (!cancelled) setIsLoading(false)
      }
    }

    load()
    return () => { cancelled = true }
  }, [])

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="sg-body-sm text-sg-text-muted">Loading dashboard…</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="sg-alert sg-alert-danger">
        <span>{error}</span>
      </div>
    )
  }

  if (!data) return null

  const summaryCards = [
    {
      label: 'Total Prosumers',
      value: data.totalProsumers,
      icon: Users,
      color: 'var(--sg-primary)',
      bg: 'var(--sg-primary-soft)',
    },
    {
      label: 'Pending Activations',
      value: data.pendingActivations,
      icon: UserCheck,
      color: 'var(--sg-warning)',
      bg: 'var(--sg-warning-soft)',
    },
    {
      label: 'Active Nodes',
      value: data.activeNodes,
      icon: Battery,
      color: 'var(--sg-success)',
      bg: 'var(--sg-success-soft)',
    },
    {
      label: 'Total Reservations',
      value:
        data.reservationsByStatus.pending +
        data.reservationsByStatus.approved +
        data.reservationsByStatus.completed +
        data.reservationsByStatus.cancelled +
        data.reservationsByStatus.expired,
      icon: Zap,
      color: 'var(--sg-cyan)',
      bg: 'var(--sg-cyan-soft)',
    },
  ]

  const statusCards = [
    { label: 'Pending', value: data.reservationsByStatus.pending, badgeClass: 'sg-badge-warning' },
    { label: 'Approved', value: data.reservationsByStatus.approved, badgeClass: 'sg-badge-info' },
    { label: 'Completed', value: data.reservationsByStatus.completed, badgeClass: 'sg-badge-success' },
    { label: 'Cancelled', value: data.reservationsByStatus.cancelled, badgeClass: 'sg-badge-danger' },
    { label: 'Expired', value: data.reservationsByStatus.expired, badgeClass: 'sg-badge-neutral' },
  ]

  return (
    <div className="sg-stack-lg">
      <div>
        <h2 className="sg-h3">Dashboard</h2>
        <p className="sg-body-sm mt-1">System overview at a glance</p>
      </div>

      {/* Summary cards */}
      <div className="sg-grid sg-grid-4">
        {summaryCards.map((card) => (
          <div key={card.label} className="sg-card sg-card-body">
            <div className="flex items-center gap-4">
              <div
                className="flex items-center justify-center rounded-lg"
                style={{
                  width: 48,
                  height: 48,
                  background: card.bg,
                  color: card.color,
                }}
              >
                <card.icon size={24} />
              </div>
              <div>
                <p className="sg-caption">{card.label}</p>
                <p className="sg-h2">{card.value}</p>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Reservations by status */}
      <div className="sg-card">
        <div className="sg-card-header">
          <h3 className="sg-h4">Reservations by Status</h3>
        </div>
        <div className="sg-card-body">
          <div className="sg-grid sg-grid-5-custom">
            {statusCards.map((s) => (
              <div key={s.label} className="text-center">
                <p className="sg-h2">{s.value}</p>
                <span className={`sg-badge ${s.badgeClass} mt-2`}>{s.label}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  )
}
