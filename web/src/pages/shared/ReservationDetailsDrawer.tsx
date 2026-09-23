import { useId } from 'react'
import {
  AlertTriangle,
  CircleCheckBig,
  Clock,
  Edit,
  Info,
  Mail,
  MapPin,
  Phone,
  QrCode,
  ShieldCheck,
  Trash2,
  User,
  Zap,
} from 'lucide-react'
import { useProsumer } from '../../api/ProsumersApi'
import { useNodes } from '../../api/NodesApi'
import { Button } from '../../components/ui/button'
import {
  Sheet,
  SheetBody,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '../../components/ui/sheet'
import type { Reservation, ReservationStatus } from '../../types/ReservationTypes'

interface ReservationDetailsDrawerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  reservation: Reservation | null
  onApprove: (res: Reservation) => void
  onModify: (res: Reservation) => void
  onCancel: (res: Reservation) => void
  isApproving?: boolean
}

function getStatusBadgeClass(status?: ReservationStatus) {
  switch (status) {
    case 'Approved':
      return 'sg-badge-success'
    case 'Pending':
      return 'sg-badge-warning'
    case 'Cancelled':
      return 'sg-badge-danger'
    case 'Completed':
      return 'sg-badge-info'
    case 'Expired':
    default:
      return 'sg-badge-neutral'
  }
}

function formatFullDate(iso?: string) {
  if (!iso) return '-'
  return new Date(iso).toLocaleDateString(undefined, {
    weekday: 'long',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })
}

function formatFullDateTime(iso?: string) {
  if (!iso) return '-'
  return new Date(iso).toLocaleString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

function formatTimeWindow(startIso?: string, endIso?: string) {
  if (!startIso || !endIso) return '-'
  const start = new Date(startIso).toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  })
  const end = new Date(endIso).toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  })
  return `${start} – ${end}`
}

function isWithinNoticeWindow(slotStartTimeIso?: string, minHours = 12): boolean {
  if (!slotStartTimeIso) return false
  const slotStart = new Date(slotStartTimeIso).getTime()
  const now = Date.now()
  const diffHours = (slotStart - now) / (1000 * 60 * 60)
  return diffHours >= minHours
}

export default function ReservationDetailsDrawer({
  open,
  onOpenChange,
  reservation,
  onApprove,
  onModify,
  onCancel,
  isApproving = false,
}: ReservationDetailsDrawerProps) {
  const titleId = useId()
  const descId = useId()
  const { data: prosumer, isLoading: isLoadingProsumer } = useProsumer(reservation?.nic ?? '')
  const { data: nodesData } = useNodes()

  if (!reservation) return null

  const node = nodesData?.items?.find((n) => n.nodeId === reservation.nodeId)
  const canNotice = isWithinNoticeWindow(reservation.slotStartTime, 12)
  const isPending = reservation.status === 'Pending'
  const isApproved = reservation.status === 'Approved'
  const isTerminal = ['Cancelled', 'Completed', 'Expired'].includes(reservation.status)

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="flex w-full flex-col p-0 sm:max-w-xl" aria-labelledby={titleId} aria-describedby={descId}>
        {/* Header */}
        <SheetHeader className="border-b border-sg-border bg-sg-surface px-6 py-5">
          <div className="flex items-start justify-between gap-4">
            <div className="flex flex-col gap-1">
              <div className="flex items-center gap-2.5">
                <SheetTitle id={titleId} className="font-mono text-xl font-bold tracking-tight text-sg-text">
                  {reservation.reservationId}
                </SheetTitle>
                <span className={`sg-badge ${getStatusBadgeClass(reservation.status)}`}>
                  {reservation.status}
                </span>
              </div>
              <SheetDescription id={descId} className="sg-caption text-sg-text-muted">
                Created on {formatFullDateTime(reservation.createdAt)}
              </SheetDescription>
            </div>
          </div>
        </SheetHeader>

        {/* Scrollable Body */}
        <SheetBody className="flex flex-1 flex-col gap-5 overflow-y-auto px-6 py-5">
          {/* 12-Hour Policy Notice Banner */}
          {!isTerminal && !canNotice && (
            <div className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50/80 p-3.5 text-amber-900 dark:border-amber-900/60 dark:bg-amber-950/30 dark:text-amber-300">
              <AlertTriangle size={18} className="mt-0.5 shrink-0 text-amber-600 dark:text-amber-400" />
              <div className="text-xs leading-relaxed">
                <span className="font-semibold">Notice Lock Active (BR-06 / BR-07):</span> Less than 12
                hours before slot start. Modifications and cancellations are locked.
              </div>
            </div>
          )}

          {/* Key Metric Overview */}
          <div className="grid grid-cols-2 gap-3">
            <div className="flex flex-col gap-1 rounded-xl border border-sg-border bg-sg-surface p-4 shadow-xs">
              <span className="sg-caption flex items-center gap-1.5 text-sg-text-muted">
                <Zap size={14} className="text-sg-primary" />
                Requested Energy
              </span>
              <p className="mt-1 text-2xl font-bold tracking-tight text-sg-text">
                {reservation.requestedKwh}{' '}
                <span className="text-xs font-normal text-sg-text-muted">kWh</span>
              </p>
            </div>

            <div className="flex flex-col gap-1 rounded-xl border border-sg-border bg-sg-surface p-4 shadow-xs">
              <span className="sg-caption flex items-center gap-1.5 text-sg-text-muted">
                <Clock size={14} className="text-sg-primary" />
                Slot Window
              </span>
              <p className="mt-1 font-mono text-sm font-semibold text-sg-text">
                {formatTimeWindow(reservation.slotStartTime, reservation.slotEndTime)}
              </p>
              <span className="text-[11px] text-sg-text-muted">
                {formatFullDate(reservation.slotStartTime)}
              </span>
            </div>
          </div>

          {/* Prosumer Details Card */}
          <div className="flex flex-col gap-3 rounded-xl border border-sg-border bg-sg-surface p-4">
            <div className="flex items-center justify-between border-b border-sg-border/60 pb-2.5">
              <div className="flex items-center gap-2">
                <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                  <User size={15} />
                </div>
                <h4 className="text-sm font-semibold text-sg-text">Prosumer Information</h4>
              </div>
              <span className="sg-caption font-mono text-sg-text-muted">NIC Account</span>
            </div>

            <div className="grid grid-cols-1 gap-2.5 text-xs sm:grid-cols-2">
              <div className="flex flex-col gap-0.5">
                <span className="text-sg-text-muted">Full Name</span>
                <span className="font-medium text-sg-text">
                  {isLoadingProsumer ? (
                    <span className="text-sg-text-muted">Loading...</span>
                  ) : prosumer?.fullName ? (
                    prosumer.fullName
                  ) : (
                    'Registered Prosumer'
                  )}
                </span>
              </div>

              <div className="flex flex-col gap-0.5">
                <span className="text-sg-text-muted">National Identity Card (NIC)</span>
                <span className="font-mono font-medium text-sg-text">{reservation.nic}</span>
              </div>

              {prosumer?.email && (
                <div className="flex flex-col gap-0.5">
                  <span className="text-sg-text-muted">Email</span>
                  <span className="flex items-center gap-1 font-medium text-sg-text">
                    <Mail size={12} className="text-sg-text-muted" />
                    {prosumer.email}
                  </span>
                </div>
              )}

              {prosumer?.phone && (
                <div className="flex flex-col gap-0.5">
                  <span className="text-sg-text-muted">Phone</span>
                  <span className="flex items-center gap-1 font-medium text-sg-text">
                    <Phone size={12} className="text-sg-text-muted" />
                    {prosumer.phone}
                  </span>
                </div>
              )}
            </div>
          </div>

          {/* Microgrid Hub / Node Card */}
          <div className="flex flex-col gap-3 rounded-xl border border-sg-border bg-sg-surface p-4">
            <div className="flex items-center justify-between border-b border-sg-border/60 pb-2.5">
              <div className="flex items-center gap-2">
                <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                  <MapPin size={15} />
                </div>
                <h4 className="text-sm font-semibold text-sg-text">Microgrid Hub Station</h4>
              </div>
              <span className="sg-caption font-mono font-semibold text-sg-text-secondary">
                {reservation.nodeId}
              </span>
            </div>

            <div className="flex flex-col gap-2 text-xs">
              <div className="flex flex-col gap-0.5">
                <span className="text-sg-text-muted">Hub Name</span>
                <span className="text-sm font-semibold text-sg-text">
                  {node?.name ?? reservation.nodeId}
                </span>
              </div>

              {node?.location?.addressLine && (
                <div className="flex items-center gap-1.5 text-sg-text-secondary">
                  <MapPin size={13} className="shrink-0 text-sg-text-muted" />
                  <span>{node.location.addressLine}</span>
                </div>
              )}

              <div className="mt-1 flex items-center gap-2">
                <span className="rounded-md bg-sg-surface-muted px-2 py-0.5 font-mono text-[11px] text-sg-text-secondary">
                  Slot ID: {reservation.slotId}
                </span>
                {node?.type && (
                  <span className="rounded-md bg-sg-surface-muted px-2 py-0.5 text-[11px] font-medium text-sg-text-secondary">
                    {node.type} Node
                  </span>
                )}
              </div>
            </div>
          </div>

          {/* Lifecycle & Status Card */}
          <div className="flex flex-col gap-3 rounded-xl border border-sg-border bg-sg-surface p-4">
            <div className="flex items-center gap-2 border-b border-sg-border/60 pb-2.5">
              <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                <ShieldCheck size={15} />
              </div>
              <h4 className="text-sm font-semibold text-sg-text">Audit & Lifecycle Details</h4>
            </div>

            <div className="flex flex-col gap-2.5 text-xs">
              <div className="flex items-center justify-between">
                <span className="text-sg-text-muted">Last Updated</span>
                <span className="font-mono text-sg-text">{formatFullDateTime(reservation.updatedAt)}</span>
              </div>

              {isApproved && (
                <div className="flex items-center justify-between border-t border-sg-border/40 pt-2 text-emerald-600 dark:text-emerald-400">
                  <span className="flex items-center gap-1.5 font-medium">
                    <QrCode size={14} />
                    Signed QR Code Token
                  </span>
                  <span className="rounded-full bg-emerald-50 px-2 py-0.5 text-[11px] font-semibold dark:bg-emerald-950/50">
                    Active & Issued
                  </span>
                </div>
              )}

              {reservation.status === 'Cancelled' && (
                <div className="flex flex-col gap-1 border-t border-sg-border/40 pt-2 text-rose-600 dark:text-rose-400">
                  <div className="flex justify-between">
                    <span>Cancelled At</span>
                    <span className="font-mono">{formatFullDateTime(reservation.cancelledAt ?? '')}</span>
                  </div>
                  {reservation.cancelledBy && (
                    <div className="flex justify-between text-sg-text-muted">
                      <span>Cancelled By</span>
                      <span className="font-mono">{reservation.cancelledBy}</span>
                    </div>
                  )}
                </div>
              )}

              {reservation.status === 'Completed' && (
                <div className="flex flex-col gap-1 border-t border-sg-border/40 pt-2 text-sky-600 dark:text-sky-400">
                  <div className="flex justify-between">
                    <span>Completed At</span>
                    <span className="font-mono">{formatFullDateTime(reservation.completedAt ?? '')}</span>
                  </div>
                  {reservation.deliveredKwh != null && (
                    <div className="flex justify-between font-semibold">
                      <span>Delivered Power</span>
                      <span>{reservation.deliveredKwh} kWh</span>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        </SheetBody>

        {/* Footer Actions */}
        <SheetFooter className="border-t border-sg-border bg-sg-surface-muted/30 p-5">
          <div className="flex w-full flex-col gap-3">
            {isPending && (
              <Button
                onClick={() => onApprove(reservation)}
                disabled={isApproving}
                className="w-full font-semibold"
              >
                <CircleCheckBig size={16} className="mr-2" />
                {isApproving ? 'Approving...' : 'Approve reservation & issue QR'}
              </Button>
            )}

            {!isTerminal && (
              <div className="grid grid-cols-2 gap-2.5">
                <Button
                  variant="outline"
                  onClick={() => onModify(reservation)}
                  disabled={!canNotice}
                  title={
                    canNotice
                      ? 'Modify slot or kWh'
                      : 'Modifications locked less than 12 hours before slot start (BR-06)'
                  }
                  className="w-full text-sg-text-secondary hover:text-sg-text"
                >
                  <Edit size={14} className="mr-1.5" />
                  Modify slot
                </Button>

                <Button
                  variant="outline"
                  onClick={() => onCancel(reservation)}
                  disabled={!canNotice}
                  title={
                    canNotice
                      ? 'Cancel reservation'
                      : 'Cancellations locked less than 12 hours before slot start (BR-07)'
                  }
                  className="w-full bg-red-50 border-red-200 text-red-600 hover:bg-red-100 hover:text-red-700 dark:border-red-900/60 dark:text-red-400 dark:hover:bg-red-950/40"
                >
                  <Trash2 size={14} className="mr-1.5" />
                  Cancel booking
                </Button>
              </div>
            )}

            {isTerminal && (
              <div className="flex items-center justify-center gap-2 rounded-lg bg-sg-surface-muted py-2 text-xs text-sg-text-muted">
                <Info size={14} />
                <span>This booking is in a final state ({reservation.status})</span>
              </div>
            )}
          </div>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  )
}
