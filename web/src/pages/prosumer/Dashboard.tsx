/**
 * File        : Dashboard.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Prosumer dashboard (REQ-VIEW 2.1, AC-1/AC-2/AC-13). Shows the
 *               active and pending counts, the next upcoming reservation and
 *               recent history - every value straight from
 *               GET /dashboard/prosumer. No count is derived here.
 */

import {
  BatteryCharging,
  CalendarClock,
  Clock,
  Inbox,
  RotateCw,
  TriangleAlert,
  Zap,
} from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card'
import MetricCard from '../../components/MetricCard'
import SolarLoader from '../../components/SolarLoader/SolarLoader'
import { useProsumerDashboard } from '../../api/DashboardApi'
import { getErrorMessage } from '../../api/errors'
import {
  formatFullDate,
  formatFullDateTime,
  formatTimeWindow,
  getStatusBadgeClass,
} from '../../lib/utils'
import type { Reservation } from '../../types/ReservationTypes'

export default function Dashboard() {
  const { data, isLoading, isError, error, refetch, isFetching } = useProsumerDashboard()

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h2 className="sg-h3">My energy at a glance</h2>
          <p className="sg-body-sm text-sg-text-secondary">
            Your reservations across the SunGrid microgrid.
          </p>
        </div>
        <button
          type="button"
          onClick={() => refetch()}
          disabled={isLoading || isFetching}
          className="sg-btn sg-btn-ghost sg-btn-sm flex-shrink-0"
          title="Refresh my dashboard"
        >
          <RotateCw size={13} className={isFetching ? 'animate-spin' : ''} />
          <span className="ml-1.5">Refresh</span>
        </button>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20">
          <SolarLoader showLabel label="Loading your dashboard..." />
        </div>
      ) : isError ? (
        <ErrorState message={getErrorMessage(error)} onRetry={() => refetch()} />
      ) : (
        <>
          {/* AC-1: both counts come from the API response, never from
              recentHistory.length or a filter over a cached list (AC-13). */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <MetricCard
              label="Active reservations"
              value={data?.activeCount ?? 0}
              icon={BatteryCharging}
              hint="Approved, still upcoming"
            />
            <MetricCard
              label="Pending reservations"
              value={data?.pendingCount ?? 0}
              icon={Clock}
              hint="Awaiting operator approval"
            />
          </div>

          <NextReservationCard reservation={data?.nextReservation ?? null} />

          <RecentHistoryCard history={data?.recentHistory ?? []} />
        </>
      )}
    </div>
  )
}

/** Page-level load failure, with a retry (ui-design-system.md section 17). */
function ErrorState({ message, onRetry }: { message: string; onRetry: () => void }) {
  // The message is already mapped to user-facing copy by getErrorMessage, so
  // no raw server text or stack trace can reach this markup.
  return (
    <Card className="border border-sg-border bg-sg-surface">
      <CardContent className="sg-error-state">
        <div className="sg-error-icon">
          <TriangleAlert size={22} />
        </div>
        <p className="sg-body font-medium text-sg-text">We could not load your dashboard</p>
        <p className="sg-body-sm text-sg-text-secondary">{message}</p>
        <button type="button" onClick={onRetry} className="sg-btn sg-btn-primary sg-btn-sm">
          Try again
        </button>
      </CardContent>
    </Card>
  )
}

/** The single next upcoming reservation, or an empty state when there is none. */
function NextReservationCard({ reservation }: { reservation: Reservation | null }) {
  return (
    <Card className="border border-sg-border bg-sg-surface shadow-sm">
      <CardHeader className="border-b border-sg-border bg-sg-surface-muted/30 pb-3">
        <CardTitle className="sg-h4">Next reservation</CardTitle>
      </CardHeader>
      <CardContent className={reservation ? 'pt-5' : 'p-0'}>
        {!reservation ? (
          // EC-1: zero reservations is a legitimate state, not a blank panel.
          <div className="sg-empty">
            <div className="sg-empty-icon">
              <CalendarClock size={22} />
            </div>
            <p className="sg-body font-medium text-sg-text">Nothing booked yet</p>
            <p className="sg-body-sm text-sg-text-secondary">
              Reserve a slot in the SunGrid Android app and it will show up here.
            </p>
          </div>
        ) : (
          <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="min-w-0 flex flex-col gap-1">
              <div className="flex items-center gap-2">
                <span className="sg-body font-semibold text-sg-text">{reservation.nodeId}</span>
                <span className={`sg-badge ${getStatusBadgeClass(reservation.status)}`}>
                  {reservation.status}
                </span>
              </div>
              <p className="sg-body-sm text-sg-text-secondary">
                {formatFullDate(reservation.slotStartTime)}
                {' · '}
                {formatTimeWindow(reservation.slotStartTime, reservation.slotEndTime)}
              </p>
              <p className="sg-caption text-sg-text-muted truncate" title={reservation.reservationId}>
                {reservation.reservationId}
              </p>
            </div>
            <div className="flex items-center gap-2 rounded-xl bg-sg-accent-soft px-4 py-3 text-sg-text">
              <Zap size={16} className="text-sg-warning" />
              <span className="sg-body font-semibold">{reservation.requestedKwh} kWh</span>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  )
}

/** Recent reservation history as returned by the API, newest first. */
function RecentHistoryCard({ history }: { history: Reservation[] }) {
  return (
    <Card className="border border-sg-border bg-sg-surface shadow-sm">
      <CardHeader className="border-b border-sg-border bg-sg-surface-muted/30 pb-3">
        <CardTitle className="sg-h4">Recent activity</CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        {history.length === 0 ? (
          <div className="sg-empty">
            <div className="sg-empty-icon">
              <Inbox size={22} />
            </div>
            <p className="sg-body font-medium text-sg-text">No history yet</p>
            <p className="sg-body-sm text-sg-text-secondary">
              Past and cancelled reservations will be listed here.
            </p>
          </div>
        ) : (
          <div className="sg-table-wrap">
            <table className="sg-table">
              <thead>
                <tr>
                  <th>Reservation</th>
                  <th>Node</th>
                  <th>Slot</th>
                  <th>Energy</th>
                  <th>Status</th>
                  <th>Updated</th>
                </tr>
              </thead>
              <tbody>
                {history.map((reservation) => (
                  <tr key={reservation.reservationId}>
                    <td className="sg-nowrap">{reservation.reservationId}</td>
                    <td className="sg-nowrap">{reservation.nodeId}</td>
                    <td className="sg-nowrap">
                      {formatFullDate(reservation.slotStartTime)}
                      {' · '}
                      {formatTimeWindow(reservation.slotStartTime, reservation.slotEndTime)}
                    </td>
                    <td className="sg-nowrap">{reservation.requestedKwh} kWh</td>
                    <td>
                      <span className={`sg-badge ${getStatusBadgeClass(reservation.status)}`}>
                        {reservation.status}
                      </span>
                    </td>
                    <td className="sg-nowrap">{formatFullDateTime(reservation.updatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </CardContent>
    </Card>
  )
}
