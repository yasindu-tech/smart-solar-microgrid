/**
 * File        : OperatorDashboard.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Grid Operator operational dashboard (DOM-04, REQ-VIEW 2.3). Shows the
 *               pending and approved-future counts and today's schedule for the
 *               operator's assigned nodes. Every figure comes from
 *               GET /dashboard/operator - node scoping is enforced by the API (BR-11),
 *               and nothing here is counted client-side (REQ-VIEW AC-13).
 * Author      : <member name - M4>
 */

import { useState } from 'react'
import { Link } from 'react-router-dom'
import {
  AlertTriangle,
  ArrowRight,
  CalendarCheck,
  CalendarClock,
  CalendarX,
  Clock,
  RefreshCw,
  ScanLine,
  User,
} from 'lucide-react'
import { useOperatorDashboard } from '../../api/DashboardApi'
import { useNodes } from '../../api/NodesApi'
import { getErrorMessage } from '../../api/errors'
import SolarLoader from '../../components/SolarLoader/SolarLoader'
import MetricCard from '../../components/MetricCard/MetricCard'
import EmptyState from '../../components/EmptyState/EmptyState'
import ErrorState from '../../components/ErrorState/ErrorState'
import { Button } from '../../components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card'
import { Label } from '../../components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../../components/ui/table'
import { formatTime, getStatusBadgeClass } from '../../lib/utils'

const ALL_NODES = 'ALL'

// Renders the operator's node-scoped counts, node filter and today's schedule.
export default function OperatorDashboard() {
  const [nodeFilter, setNodeFilter] = useState<string>(ALL_NODES)

  // The unfiltered call supplies the full list of assigned nodes for the filter; when no
  // filter is set both hooks share one query key, so only one request is made.
  const { data: assignedScope } = useOperatorDashboard()
  const { data, error, isLoading, isError, isFetching, refetch, dataUpdatedAt } =
    useOperatorDashboard(nodeFilter === ALL_NODES ? undefined : { nodeId: nodeFilter })

  const { data: nodesData } = useNodes()
  const nodes = nodesData?.items ?? []
  const nodeName = (nodeId: string) => nodes.find((n) => n.nodeId === nodeId)?.name ?? nodeId

  // The API's "today" is a whole UTC day, so the label is rendered in UTC to match it.
  const todayUtcLabel = new Date().toLocaleDateString(undefined, {
    weekday: 'long',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'UTC',
  })

  const assignedNodeIds = assignedScope?.scopedNodeIds ?? []
  const todaysReservations = data?.todaysReservations ?? []

  const metrics = [
    {
      label: 'Pending Reservations',
      value: data?.pendingCount ?? 0,
      icon: Clock,
      href: '/operator/reservations?status=Pending',
      hint: 'Awaiting approval at your nodes',
    },
    {
      label: 'Approved Upcoming',
      value: data?.approvedFutureCount ?? 0,
      icon: CalendarCheck,
      href: '/operator/reservations?status=Approved',
      hint: 'Approved bookings with a future slot',
    },
  ]

  return (
    <div className="flex flex-col gap-6">
      {/* Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 className="sg-h3">Operations overview</h2>
          <p className="sg-body-sm text-sg-text-secondary">
            Booking counts and today&apos;s schedule for the nodes you are assigned to.
          </p>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row sm:items-end">
          {assignedNodeIds.length > 1 && (
            <div className="flex flex-col gap-1.5 sm:w-56">
              <Label htmlFor="operator-node-filter" className="sg-caption">
                Node
              </Label>
              <Select value={nodeFilter} onValueChange={setNodeFilter}>
                <SelectTrigger id="operator-node-filter">
                  <SelectValue placeholder="All my nodes" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={ALL_NODES}>All my nodes</SelectItem>
                  {assignedNodeIds.map((nodeId) => (
                    <SelectItem key={nodeId} value={nodeId}>
                      {nodeName(nodeId)} ({nodeId})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          <Button
            variant="outline"
            onClick={() => refetch()}
            disabled={isFetching}
            className="w-fit"
          >
            <RefreshCw size={15} />
            {isFetching ? 'Refreshing...' : 'Refresh'}
          </Button>
        </div>
      </div>

      {/* Verification happens on-site in the Android app, never from a web or cached decision (BR-10, BR-14). */}
      <div className="sg-alert sg-alert-info">
        <ScanLine size={18} className="mt-0.5 shrink-0" aria-hidden="true" />
        <p className="sg-body-sm">
          QR verification and transfer completion are done on-site in the SunGrid Android app.
          Scan the prosumer&apos;s code at the node - the server checks it before anything is
          recorded.
        </p>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-20">
          <SolarLoader showLabel label="Loading operations dashboard..." />
        </div>
      ) : isError ? (
        <Card className="border border-sg-border bg-sg-surface">
          <ErrorState
            title="Could not load the dashboard"
            message={getErrorMessage(error)}
            onRetry={() => refetch()}
            isRetrying={isFetching}
          />
        </Card>
      ) : (
        <>
          {data && data.scopedNodeIds.length === 0 && (
            <div className="sg-alert sg-alert-warning" role="status">
              <AlertTriangle size={18} className="mt-0.5 shrink-0" aria-hidden="true" />
              <p className="sg-body-sm">
                You are not assigned to any microgrid node yet, so there is nothing to show. Ask
                a Backoffice administrator to assign you to a node.
              </p>
            </div>
          )}

          {/* API-computed counts */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            {metrics.map((metric) => (
              <MetricCard key={metric.label} {...metric} />
            ))}
          </div>

          {/* Today's schedule */}
          <Card className="overflow-hidden border border-sg-border bg-sg-surface shadow-xs">
            <CardHeader className="border-b border-sg-border bg-sg-surface-muted/30 pb-3">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                <div className="flex flex-col gap-1">
                  <CardTitle className="sg-h4">Today&apos;s schedule</CardTitle>
                  <p className="sg-caption text-sg-text-muted">
                    Slots starting on {todayUtcLabel} (UTC)
                    {dataUpdatedAt > 0 &&
                      ` · updated ${formatTime(new Date(dataUpdatedAt).toISOString())}`}
                  </p>
                </div>
                <Link
                  to="/operator/reservations"
                  className="flex items-center gap-1 text-xs font-medium text-sg-primary hover:underline"
                >
                  View all reservations
                  <ArrowRight size={13} />
                </Link>
              </div>
            </CardHeader>

            <CardContent className="p-0">
              {todaysReservations.length === 0 ? (
                <EmptyState
                  icon={CalendarX}
                  title="No reservations today"
                  description="Bookings with a slot starting today at your nodes will appear here."
                />
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow className="border-b border-sg-border bg-sg-surface-muted/40 hover:bg-sg-surface-muted/40">
                      <TableHead className="font-semibold text-sg-text">Slot Window</TableHead>
                      <TableHead className="font-semibold text-sg-text">ID</TableHead>
                      <TableHead className="font-semibold text-sg-text">Prosumer NIC</TableHead>
                      <TableHead className="font-semibold text-sg-text">Microgrid Node</TableHead>
                      <TableHead className="font-semibold text-sg-text">Energy</TableHead>
                      <TableHead className="font-semibold text-sg-text">Status</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {todaysReservations.map((res) => (
                      <TableRow
                        key={res.reservationId}
                        className="border-b border-sg-border/60 hover:bg-sg-primary-soft/20"
                      >
                        <TableCell>
                          <div className="flex items-center gap-1.5">
                            <CalendarClock size={13} className="text-sg-text-muted" />
                            <span className="font-mono text-xs font-medium text-sg-text">
                              {formatTime(res.slotStartTime)} – {formatTime(res.slotEndTime)}
                            </span>
                          </div>
                        </TableCell>

                        <TableCell className="font-mono text-xs font-semibold text-sg-primary">
                          {res.reservationId}
                        </TableCell>

                        <TableCell>
                          <div className="flex items-center gap-1.5">
                            <User size={13} className="text-sg-text-muted" />
                            <span className="font-mono text-xs font-medium text-sg-text">
                              {res.nic}
                            </span>
                          </div>
                        </TableCell>

                        <TableCell>
                          <div className="flex flex-col">
                            <span className="text-xs font-semibold text-sg-text">
                              {nodeName(res.nodeId)}
                            </span>
                            <span className="font-mono text-[11px] text-sg-text-muted">
                              {res.nodeId}
                            </span>
                          </div>
                        </TableCell>

                        <TableCell>
                          <div className="flex flex-col">
                            <span className="font-semibold text-sg-text">
                              {res.requestedKwh}{' '}
                              <span className="text-xs font-normal text-sg-text-muted">
                                kWh requested
                              </span>
                            </span>
                            {res.status === 'Completed' && res.deliveredKwh != null && (
                              <span className="text-[11px] text-sg-text-muted">
                                {res.deliveredKwh} kWh delivered
                              </span>
                            )}
                          </div>
                        </TableCell>

                        <TableCell>
                          <span className={`sg-badge ${getStatusBadgeClass(res.status)}`}>
                            {res.status}
                          </span>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </>
      )}
    </div>
  )
}
