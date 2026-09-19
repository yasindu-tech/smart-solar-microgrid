import { Users, Clock, Battery, CalendarCheck } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card'

const STATS = [
  { label: 'Total Prosumers', value: 0, icon: Users },
  { label: 'Pending Activations', value: 0, icon: Clock },
  { label: 'Active Nodes', value: 0, icon: Battery },
  { label: 'Total Reservations', value: 0, icon: CalendarCheck },
] as const

const RESERVATIONS_BY_STATUS = [
  { status: 'Pending', count: 0 },
  { status: 'Approved', count: 0 },
  { status: 'Completed', count: 0 },
  { status: 'Cancelled', count: 0 },
] as const

export default function Dashboard() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h2 className="sg-h3">Overview</h2>
        <p className="sg-body-sm text-sg-text-secondary">
          Snapshot of prosumers, nodes and reservations across the grid.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {STATS.map(({ label, value, icon: Icon }) => (
          <Card key={label}>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium text-sg-text-secondary">{label}</CardTitle>
              <Icon size={18} className="text-sg-primary" />
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-semibold text-sg-text">{value}</p>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="sg-h4">Reservations by status</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
            {RESERVATIONS_BY_STATUS.map(({ status, count }) => (
              <div key={status} className="rounded-md border border-sg-border p-4">
                <p className="text-xs uppercase tracking-wide text-sg-text-muted">{status}</p>
                <p className="mt-1 text-xl font-semibold text-sg-text">{count}</p>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
