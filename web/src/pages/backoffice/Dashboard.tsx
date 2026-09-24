import { Link } from 'react-router-dom'
import { Users, Clock, Battery, CalendarCheck, ArrowRight } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card'
import { useBackofficeDashboard } from '../../api/DashboardApi'
import SolarLoader from '../../components/SolarLoader/SolarLoader'

export default function Dashboard() {
  const { data, isLoading, isError } = useBackofficeDashboard()

  const stats = [
    {
      label: 'Total Prosumers',
      value: data?.totalProsumers ?? 0,
      icon: Users,
      href: '/backoffice/users',
      hint: 'Registered prosumers',
    },
    {
      label: 'Pending Activations',
      value: data?.pendingActivations ?? 0,
      icon: Clock,
      href: '/backoffice/approvals',
      hint: 'Pending prosumer registrations',
    },
    {
      label: 'Active Nodes',
      value: data?.activeNodes ?? 0,
      icon: Battery,
      href: '/backoffice/nodes',
      hint: 'Available solar hubs',
    },
    {
      label: 'Total Reservations',
      value: data?.totalReservations ?? 0,
      icon: CalendarCheck,
      href: '/backoffice/reservations',
      hint: 'Grid energy bookings',
    },
  ]

  const byStatus = data?.reservationsByStatus ?? {}

  const reservationsByStatus = [
    {
      status: 'Pending',
      count: byStatus.Pending ?? byStatus.pending ?? 0,
      color: 'text-amber-700 dark:text-amber-500',
    },
    {
      status: 'Approved',
      count: byStatus.Approved ?? byStatus.approved ?? 0,
      color: 'text-emerald-700 dark:text-emerald-500',
    },
    {
      status: 'Completed',
      count: byStatus.Completed ?? byStatus.completed ?? 0,
      color: 'text-sky-700 dark:text-sky-500',
    },
    {
      status: 'Cancelled',
      count: byStatus.Cancelled ?? byStatus.cancelled ?? 0,
      color: 'text-red-700 dark:text-red-500',
    },
  ]

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h2 className="sg-h3">Overview</h2>
        <p className="sg-body-sm text-sg-text-secondary">
          Snapshot of prosumers, nodes and reservations across the grid.
        </p>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20">
          <SolarLoader showLabel label="Loading dashboard metrics..." />
        </div>
      ) : isError ? (
        <div className="sg-alert sg-alert-danger" role="alert">
          Could not load dashboard metrics. Please verify the backend connection and try again.
        </div>
      ) : (
        <>
          {/* 4 Main Metric Cards */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {stats.map(({ label, value, icon: Icon, href, hint }) => (
              <Link
                key={label}
                to={href}
                className="group block transition-transform focus:outline-none"
              >
                <Card
                  className={`h-full border border-sg-border bg-sg-surface transition-all group-hover:border-sg-primary/60 group-hover:shadow-sm`}
                >
                  <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                    <CardTitle className="text-sm font-medium text-sg-text-secondary">
                      {label}
                    </CardTitle>
                    <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary transition-colors group-hover:bg-sg-primary group-hover:text-white">
                      <Icon size={16} />
                    </div>
                  </CardHeader>
                  <CardContent>
                    <div className="flex items-baseline justify-between">
                      <p className="text-3xl font-bold tracking-tight text-sg-text">{value}</p>
                      <ArrowRight
                        size={15}
                        className="text-sg-text-muted opacity-0 transition-all group-hover:translate-x-0.5 group-hover:text-sg-primary group-hover:opacity-100"
                      />
                    </div>
                    {hint && (
                      <p className="mt-1 text-xs text-sg-text-muted">{hint}</p>
                    )}
                  </CardContent>
                </Card>
              </Link>
            ))}
          </div>

          {/* Reservations by status Card */}
          <Card className="border border-sg-border bg-sg-surface shadow-xs">
            <CardHeader className="border-b border-sg-border bg-sg-surface-muted/30 pb-3">
              <div className="flex items-center justify-between">
                <CardTitle className="sg-h4">Reservations by status</CardTitle>
                <Link
                  to="/backoffice/reservations"
                  className="flex items-center gap-1 text-xs font-medium text-sg-primary hover:underline"
                >
                  View all bookings
                  <ArrowRight size={13} />
                </Link>
              </div>
            </CardHeader>
            <CardContent className="pt-5">
              <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
                {reservationsByStatus.map(({ status, count }) => (
                  <Link
                    key={status}
                    to={`/backoffice/reservations?status=${encodeURIComponent(status)}`}
                    className="group block rounded-xl border border-sg-border bg-sg-surface-muted/20 p-4 transition-all hover:bg-sg-surface hover:shadow-xs hover:border-blue-600/40"
                  >
                    <div className="flex items-center justify-between">
                      <p className="text-xs font-semibold uppercase tracking-wider text-sg-text-muted">
                        {status}
                      </p>
                    </div>
                    <p className="mt-2 text-2xl font-bold tracking-tight text-sg-text">
                      {count}
                    </p>
                  </Link>
                ))}
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </div>
  )
}
