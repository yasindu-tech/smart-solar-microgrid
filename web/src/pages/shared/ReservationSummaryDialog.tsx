import { CheckCircle2, AlertCircle, Calendar, Clock, MapPin, Zap, User, Hash } from 'lucide-react'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '../../components/ui/dialog'
import { Button } from '../../components/ui/button'
import type { ReservationSummaryDialogProps } from '../../types/ReservationTypes'
import { getStatusBadgeClass, formatSlotDateTime, formatTimeWindow } from '../../lib/utils'

export default function ReservationSummaryDialog({
  open,
  onOpenChange,
  reservation,
  action,
}: ReservationSummaryDialogProps) {
  if (!reservation) return null

  const isCancelled = action === 'Cancelled'

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div
              className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${
                isCancelled
                  ? 'bg-rose-50 text-rose-600 dark:bg-rose-950/40 dark:text-rose-400'
                  : 'bg-sg-primary-soft text-sg-primary'
              }`}
            >
              {isCancelled ? <AlertCircle size={20} /> : <CheckCircle2 size={20} />}
            </div>
            <div>
              <DialogTitle>Reservation {action}</DialogTitle>
              <DialogDescription>
                Energy transfer reservation details and current status.
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        <div className="mt-4 flex flex-col gap-4">
          {/* Header Summary Box */}
          <div className="rounded-lg border border-sg-border bg-sg-surface-muted/40 p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-1.5 text-sg-text-muted">
                <Hash size={14} />
                <span className="sg-caption font-mono uppercase">Reservation ID</span>
              </div>
              <span className="font-mono text-sm font-semibold text-sg-text">
                {reservation.reservationId}
              </span>
            </div>

            <div className="mt-2.5 flex items-center justify-between border-t border-sg-border/60 pt-2.5">
              <span className="sg-caption text-sg-text-muted">Status</span>
              <span className={`sg-badge ${getStatusBadgeClass(reservation.status)}`}>
                {reservation.status}
              </span>
            </div>
          </div>

          {/* Details Grid */}
          <div className="grid grid-cols-2 gap-3 text-sm">
            <div className="flex flex-col gap-1 rounded-md border border-sg-border p-3">
              <div className="flex items-center gap-1.5 text-sg-text-muted">
                <User size={13} />
                <span className="sg-caption">Prosumer NIC</span>
              </div>
              <span className="font-mono font-medium text-sg-text">{reservation.nic}</span>
            </div>

            <div className="flex flex-col gap-1 rounded-md border border-sg-border p-3">
              <div className="flex items-center gap-1.5 text-sg-text-muted">
                <MapPin size={13} />
                <span className="sg-caption">Node</span>
              </div>
              <span className="font-mono font-medium text-sg-text">{reservation.nodeId}</span>
            </div>

            <div className="flex flex-col gap-1 rounded-md border border-sg-border p-3">
              <div className="flex items-center gap-1.5 text-sg-text-muted">
                <Calendar size={13} />
                <span className="sg-caption">Slot Date</span>
              </div>
              <span className="font-medium text-sg-text">
                {formatSlotDateTime(reservation.slotStartTime)}
              </span>
            </div>

            <div className="flex flex-col gap-1 rounded-md border border-sg-border p-3">
              <div className="flex items-center gap-1.5 text-sg-text-muted">
                <Clock size={13} />
                <span className="sg-caption">Window</span>
              </div>
              <span className="font-mono font-medium text-sg-text">
                {formatTimeWindow(reservation.slotStartTime, reservation.slotEndTime)}
              </span>
            </div>
          </div>

          {/* Energy Quota Box */}
          <div className="flex items-center justify-between rounded-lg border border-sg-border bg-sg-surface p-3.5">
            <div className="flex items-center gap-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-md bg-sg-primary-soft text-sg-primary">
                <Zap size={16} />
              </div>
              <div>
                <p className="sg-caption text-sg-text-muted">Requested Transfer</p>
                <p className="text-base font-semibold text-sg-text">
                  {reservation.requestedKwh} <span className="text-xs font-normal text-sg-text-muted">kWh</span>
                </p>
              </div>
            </div>

            <span className="sg-caption font-mono text-sg-text-secondary">
              Slot ID: {reservation.slotId}
            </span>
          </div>
        </div>

        <DialogFooter className="mt-4">
          <Button onClick={() => onOpenChange(false)} className="w-full sm:w-auto">
            Close
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
