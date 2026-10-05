import { Link } from 'react-router-dom'
import { Users, Clock, Battery, CalendarCheck, ArrowRight, RotateCw } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card'
import { useBackofficeDashboard } from '../../api/DashboardApi'
import SolarLoader from '../../components/SolarLoader/SolarLoader'
import MetricCard from '../../components/MetricCard/MetricCard'

export default function Dashboard() {
  const { data, isLoading, isError, refetch, isFetching } = useBackofficeDashboard()

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
    },
    {
      status: 'Approved',
      count: byStatus.Approved ?? byStatus.approved ?? 0,
    },
    {
      status: 'Completed',
      count: byStatus.Completed ?? byStatus.completed ?? 0,
    },
    {
      status: 'Cancelled',
      count: byStatus.Cancelled ?? byStatus.cancelled ?? 0,
    },
    {
      status: 'Expired',
      count: byStatus.Expired ?? byStatus.expired ?? 0,
    },
  ]

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-start justify-between">
        <div>
          <h2 className="sg-h3">Overview</h2>
          <p className="sg-body-sm text-sg-text-secondary">
            Snapshot of prosumers, nodes and reservations across the grid.
          </p>
        </div>
        <button
          type="button"
          onClick={() => refetch()}
          disabled={isLoading || isFetching}
          className="flex items-center gap-1.5 rounded-lg border border-sg-border bg-sg-surface px-3 py-1.5 text-xs font-medium text-sg-text-secondary transition-colors hover:bg-sg-surface-muted hover:text-sg-text focus:outline-none disabled:opacity-50"
          title="Refresh metrics"
        >
          <RotateCw size={13} className={isFetching ? 'animate-spin' : ''} />
          <span>Refresh</span>
        </button>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20">
          <SolarLoader showLabel label="Loading dashboard metrics..." />
        </div>
      ) : isError ? (
        <div className="sg-alert sg-alert-danger flex items-center justify-between" role="alert">
          <span>Could not load dashboard metrics. Please verify the backend connection and try again.</span>
          <button
            type="button"
            onClick={() => refetch()}
            className="ml-4 rounded-md border border-sg-danger/40 bg-white/80 px-2.5 py-1 text-xs font-semibold text-sg-danger hover:bg-white"
          >
            Retry
          </button>
        </div>
      ) : (
        <>
          {/* 4 Main Metric Cards */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {stats.map((stat) => (
              <MetricCard key={stat.label} {...stat} />
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
              <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
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
