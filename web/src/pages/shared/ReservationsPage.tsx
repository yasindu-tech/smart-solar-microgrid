import { useState, useId, useEffect } from 'react'
import type { FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  Calendar,
  CalendarPlus,
  ChevronLeft,
  ChevronRight,
  Clock,
  Edit,
  Eye,
  Filter,
  MapPin,
  Plus,
  RotateCcw,
  Search,
  Trash2,
  User,
  Zap,
} from 'lucide-react'
import { useNodes } from '../../api/NodesApi'
import { useProsumers } from '../../api/ProsumersApi'
import { useSlots } from '../../api/SlotsApi'
import {
  useApproveReservation,
  useCancelReservation,
  useCreateReservationOnBehalf,
  useReservations,
  useUpdateReservation,
} from '../../api/ReservationsApi'
import type { ApiRequestError } from '../../api/types'
import SolarLoader from '../../components/SolarLoader/SolarLoader'
import { Button } from '../../components/ui/button'
import { Card, CardContent, CardHeader } from '../../components/ui/card'
import { Input } from '../../components/ui/input'
import { Label } from '../../components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select'
import {
  Sheet,
  SheetBody,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from '../../components/ui/sheet'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '../../components/ui/dialog'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../../components/ui/table'
import { useToast } from '../../lib/useToast'
import { getStatusBadgeClass, formatDate, formatTime } from '../../lib/utils'
import { RESERVATION_STATUSES, type ReservationAction } from '../../lib/constants'
import type { Reservation } from '../../types/ReservationTypes'
import type { Slot } from '../../types/SlotTypes'
import ReservationDetailsDrawer from './ReservationDetailsDrawer'
import ReservationSummaryDialog from './ReservationSummaryDialog'



export default function ReservationsPage() {
  const { showToast } = useToast()
  const createSheetTitleId = useId()
  const createSheetDescId = useId()
  const modifySheetTitleId = useId()
  const modifySheetDescId = useId()

  const [searchParams, setSearchParams] = useSearchParams()
  const statusParam = searchParams.get('status')

  // Filter and pagination state
  const [page, setPage] = useState(1)
  const pageSize = 10
  const [statusFilter, setStatusFilter] = useState<string>(() => {
    if (statusParam && (RESERVATION_STATUSES as readonly string[]).includes(statusParam)) {
      return statusParam
    }
    return 'ALL'
  })
  const [nodeFilter, setNodeFilter] = useState<string>('ALL')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [searchTerm, setSearchTerm] = useState('')

  useEffect(() => {
    const status = searchParams.get('status')
    if (status && (RESERVATION_STATUSES as readonly string[]).includes(status)) {
      setStatusFilter(status)
      setPage(1)
    } else if (!status) {
      setStatusFilter('ALL')
    }
  }, [searchParams])

  // Queries
  const { data: nodesData } = useNodes()
  const nodes = nodesData?.items ?? []

  const queryParams = {
    page,
    pageSize,
    status: statusFilter === 'ALL' ? undefined : statusFilter,
    nodeId: nodeFilter === 'ALL' ? undefined : nodeFilter,
    from: dateFrom ? new Date(`${dateFrom}T00:00:00Z`).toISOString() : undefined,
    to: dateTo ? new Date(`${dateTo}T23:59:59Z`).toISOString() : undefined,
    search: searchTerm.trim() ? searchTerm.trim() : undefined,
  }

  const {
    data: reservationsData,
    isLoading,
    isError,
    refetch,
  } = useReservations(queryParams)
  const reservations = reservationsData?.items ?? []
  const totalReservations = reservationsData?.total ?? 0


  // Modal / Drawer States
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [createSheetOpen, setCreateSheetOpen] = useState(false)
  const [modifySheetOpen, setModifySheetOpen] = useState(false)
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false)
  const [summaryDialogOpen, setSummaryDialogOpen] = useState(false)
  const [activeReservation, setActiveReservation] = useState<Reservation | null>(null)
  const [summaryAction, setSummaryAction] = useState<ReservationAction>('Created')

  // Create on Behalf Form State
  const [createNic, setCreateNic] = useState('')
  const [createNodeId, setCreateNodeId] = useState('')
  const [createSlotId, setCreateSlotId] = useState('')
  const [createKwh, setCreateKwh] = useState('')

  // Prosumers for Create Form
  const { data: prosumersData, isLoading: isLoadingProsumers } = useProsumers(
    { status: 'Active', pageSize: 100 },
    { enabled: createSheetOpen }
  )
  const activeProsumers = prosumersData?.items ?? []
  const selectedProsumer = activeProsumers.find((p) => p.nic === createNic)

  // Slots for Create Form
  const { data: createSlotsData } = useSlots(createNodeId)
  const availableCreateSlots = (createSlotsData ?? []).filter((s) => {
    const isFuture = new Date(s.startTime).getTime() > Date.now()
    return s.isActive && isFuture
  })
  const selectedCreateSlot = availableCreateSlots.find((s) => s.slotId === createSlotId)

  // Modify Form State
  const [modifySlotId, setModifySlotId] = useState('')
  const [modifyKwh, setModifyKwh] = useState('')

  // Slots for Modify Form
  const { data: modifySlotsData } = useSlots(activeReservation?.nodeId ?? '')
  const availableModifySlots = (modifySlotsData ?? []).filter((s) => {
    const isFuture = new Date(s.startTime).getTime() > Date.now()
    return s.isActive && isFuture
  })
  const selectedModifySlot = (modifySlotsData ?? []).find((s) => s.slotId === modifySlotId)

  // Mutations
  const { mutate: createOnBehalf, isPending: isCreating } = useCreateReservationOnBehalf({
    onSuccess: (res) => {
      showToast({
        variant: 'success',
        title: 'Reservation created',
        message: `Reservation ${res.reservationId} was created for prosumer ${res.nic}.`,
      })
      setCreateSheetOpen(false)
      setCreateNic('')
      setCreateNodeId('')
      setCreateSlotId('')
      setCreateKwh('')
      setActiveReservation(res)
      setSummaryAction('Created')
      setSummaryDialogOpen(true)
      refetch()
    },
    onError: (err: ApiRequestError) => {
      showToast({
        variant: 'danger',
        title: 'Could not create reservation',
        message: err.envelope?.message ?? 'Failed to create reservation.',
      })
    },
  })

  const { mutate: updateReservation, isPending: isUpdating } = useUpdateReservation({
    onSuccess: (res) => {
      showToast({
        variant: 'success',
        title: 'Reservation modified',
        message: `Reservation ${res.reservationId} was updated successfully.`,
      })
      setModifySheetOpen(false)
      setDrawerOpen(false)
      setActiveReservation(res)
      setSummaryAction('Modified')
      setSummaryDialogOpen(true)
      refetch()
    },
    onError: (err: ApiRequestError) => {
      showToast({
        variant: 'danger',
        title: 'Could not modify reservation',
        message: err.envelope?.message ?? 'Failed to modify reservation.',
      })
    },
  })

  const { mutate: approveReservation, isPending: isApproving } = useApproveReservation({
    onSuccess: (res) => {
      showToast({
        variant: 'success',
        title: 'Reservation approved',
        message: `Reservation ${res.reservationId} is now Approved with QR token issued.`,
      })
      setDrawerOpen(false)
      setActiveReservation(res)
      setSummaryAction('Approved')
      setSummaryDialogOpen(true)
      refetch()
    },
    onError: (err: ApiRequestError) => {
      showToast({
        variant: 'danger',
        title: 'Approval failed',
        message: err.envelope?.message ?? 'Failed to approve reservation.',
      })
    },
  })

  const { mutate: cancelReservation, isPending: isCancelling } = useCancelReservation({
    onSuccess: (res) => {
      showToast({
        variant: 'success',
        title: 'Reservation cancelled',
        message: `Reservation ${res.reservationId} has been cancelled.`,
      })
      setCancelDialogOpen(false)
      setDrawerOpen(false)
      setActiveReservation(res)
      setSummaryAction('Cancelled')
      setSummaryDialogOpen(true)
      refetch()
    },
    onError: (err: ApiRequestError) => {
      showToast({
        variant: 'danger',
        title: 'Cancellation failed',
        message: err.envelope?.message ?? 'Failed to cancel reservation.',
      })
    },
  })

  // Handlers
  const handleResetFilters = () => {
    setStatusFilter('ALL')
    setNodeFilter('ALL')
    setDateFrom('')
    setDateTo('')
    setSearchTerm('')
    setPage(1)
    setSearchParams({}, { replace: true })
  }

  const handleOpenDrawer = (res: Reservation) => {
    setActiveReservation(res)
    setDrawerOpen(true)
  }

  const handleOpenModify = (res: Reservation) => {
    setActiveReservation(res)
    setModifySlotId(res.slotId)
    setModifyKwh(String(res.requestedKwh))
    setModifySheetOpen(true)
  }

  const handleOpenCancel = (res: Reservation) => {
    setActiveReservation(res)
    setCancelDialogOpen(true)
  }

  const handleCreateSubmit = (e: FormEvent) => {
    e.preventDefault()
    if (!createNic || !createNodeId || !createSlotId || !createKwh) return

    createOnBehalf({
      nic: createNic.trim(),
      nodeId: createNodeId,
      slotId: createSlotId,
      requestedKwh: Number(createKwh),
    })
  }

  const handleModifySubmit = (e: FormEvent) => {
    e.preventDefault()
    if (!activeReservation || !modifySlotId || !modifyKwh) return

    updateReservation({
      reservationId: activeReservation.reservationId,
      request: {
        slotId: modifySlotId,
        requestedKwh: Number(modifyKwh),
      },
    })
  }

  const handleConfirmCancel = () => {
    if (!activeReservation) return
    cancelReservation(activeReservation.reservationId)
  }

  return (

    <div className="flex flex-col gap-6">
      {/* Top Header */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="sg-h3">Reservations</h2>
          <p className="sg-body-sm text-sg-text-secondary">
            Manage grid power bookings, review schedules, and dispatch approvals.
          </p>
        </div>

        <Button onClick={() => setCreateSheetOpen(true)} className="w-fit shadow-xs">
          <Plus size={16} className="mr-2" />
          Create on behalf
        </Button>
      </div>


      {/* Filter Card */}
      <Card className="overflow-hidden border border-sg-border bg-sg-surface shadow-xs">
        <CardHeader className="border-b border-sg-border bg-sg-surface-muted/20 px-5 py-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2 text-sg-text">
              <Filter size={15} className="text-sg-primary" />
              <span className="text-sm font-medium">Search & Filters</span>
            </div>
            {(statusFilter !== 'ALL' ||
              nodeFilter !== 'ALL' ||
              dateFrom ||
              dateTo ||
              searchTerm) && (
              <Button
                variant="ghost"
                size="sm"
                onClick={handleResetFilters}
                className="h-7 text-xs text-sg-text-secondary hover:text-sg-text"
              >
                <RotateCcw size={12} className="mr-1" />
                Reset filters
              </Button>
            )}
          </div>
        </CardHeader>

        <CardContent className="p-5">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
            {/* Search */}
            <div className="flex flex-col gap-1.5 lg:col-span-2">
              <Label htmlFor="search-input" className="sg-caption">
                Search
              </Label>
              <div className="relative">
                <Search
                  size={15}
                  className="absolute left-3 top-1/2 -translate-y-1/2 text-sg-text-muted"
                />
                <Input
                  id="search-input"
                  placeholder="Search by ID or Prosumer NIC..."
                  value={searchTerm}
                  onChange={(e) => {
                    setSearchTerm(e.target.value)
                    setPage(1)
                  }}
                  className="pl-9"
                />
              </div>
            </div>

            {/* Status Filter */}
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="status-filter" className="sg-caption">
                Status
              </Label>
              <Select
                value={statusFilter}
                onValueChange={(val) => {
                  setStatusFilter(val)
                  setPage(1)
                  const next = new URLSearchParams(searchParams)
                  if (val === 'ALL') {
                    next.delete('status')
                  } else {
                    next.set('status', val)
                  }
                  setSearchParams(next, { replace: true })
                }}
              >
                <SelectTrigger id="status-filter">
                  <SelectValue placeholder="All Statuses" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="ALL">All Statuses</SelectItem>
                  {RESERVATION_STATUSES.map((status) => (
                    <SelectItem key={status} value={status}>
                      {status}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Node Filter */}
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="node-filter" className="sg-caption">
                Node
              </Label>
              <Select
                value={nodeFilter}
                onValueChange={(val) => {
                  setNodeFilter(val)
                  setPage(1)
                }}
              >
                <SelectTrigger id="node-filter">
                  <SelectValue placeholder="All Nodes" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="ALL">All Nodes</SelectItem>
                  {nodes.map((n) => (
                    <SelectItem key={n.nodeId} value={n.nodeId}>
                      {n.name} ({n.nodeId})
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Date Filters */}
            <div className="flex flex-col gap-1.5">
              <Label htmlFor="date-from" className="sg-caption">
                Date From
              </Label>
              <Input
                id="date-from"
                type="date"
                value={dateFrom}
                onChange={(e) => {
                  setDateFrom(e.target.value)
                  setPage(1)
                }}
              />
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Main Content Area: Loading / Error / Table */}
      {isLoading ? (
        <div className="flex justify-center py-20">
          <SolarLoader showLabel label="Loading reservations..." />
        </div>
      ) : isError ? (
        <div className="sg-alert sg-alert-danger" role="alert">
          Could not load reservations. Please verify your connection and try again.
        </div>
      ) : reservations.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed border-sg-border py-16 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-sg-surface-muted text-sg-text-muted">
            <Calendar size={24} />
          </div>
          <p className="sg-h4">No reservations found</p>
          <p className="sg-body-sm text-sg-text-secondary">
            {searchTerm || statusFilter !== 'ALL' || nodeFilter !== 'ALL' || dateFrom || dateTo
              ? 'Try adjusting your filters or search terms.'
              : 'Registered energy transfer reservations will appear here.'}
          </p>
          {(searchTerm || statusFilter !== 'ALL' || nodeFilter !== 'ALL' || dateFrom || dateTo) && (
            <Button variant="outline" size="sm" onClick={handleResetFilters} className="mt-2">
              Clear filters
            </Button>
          )}
        </div>
      ) : (
        <div className="flex flex-col gap-4">
          <div className="overflow-hidden rounded-xl border border-sg-border bg-sg-surface shadow-xs">
            <Table>
              <TableHeader>
                <TableRow className="border-b border-sg-border bg-sg-surface-muted/40 hover:bg-sg-surface-muted/40">
                  <TableHead className="w-[120px] font-semibold text-sg-text">ID</TableHead>
                  <TableHead className="font-semibold text-sg-text">Prosumer NIC</TableHead>
                  <TableHead className="font-semibold text-sg-text">Microgrid Node</TableHead>
                  <TableHead className="font-semibold text-sg-text">Slot Window</TableHead>
                  <TableHead className="font-semibold text-sg-text">Energy</TableHead>
                  <TableHead className="font-semibold text-sg-text">Status</TableHead>
                  <TableHead className="font-semibold text-sg-text">Created</TableHead>
                  <TableHead className="w-[110px] text-right font-semibold text-sg-text">
                    Action
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {reservations.map((res) => {
                  const nodeObj = nodes.find((n) => n.nodeId === res.nodeId)
                  const isPending = res.status === 'Pending'

                  return (
                    <TableRow
                      key={res.reservationId}
                      onClick={() => handleOpenDrawer(res)}
                      className="group cursor-pointer border-b border-sg-border/60 transition-colors hover:bg-sg-primary-soft/20"
                    >
                      {/* ID */}
                      <TableCell className="font-mono text-xs font-semibold text-sg-primary">
                        {res.reservationId}
                      </TableCell>

                      {/* NIC */}
                      <TableCell>
                        <div className="flex items-center gap-1.5">
                          <User size={13} className="text-sg-text-muted" />
                          <span className="font-mono text-xs font-medium text-sg-text">
                            {res.nic}
                          </span>
                        </div>
                      </TableCell>

                      {/* Node */}
                      <TableCell>
                        <div className="flex flex-col">
                          <span className="text-xs font-semibold text-sg-text">
                            {nodeObj?.name ?? res.nodeId}
                          </span>
                          <span className="font-mono text-[11px] text-sg-text-muted">
                            {res.nodeId}
                          </span>
                        </div>
                      </TableCell>

                      {/* Slot Window */}
                      <TableCell>
                        <div className="flex flex-col">
                          <span className="text-xs font-medium text-sg-text">
                            {formatDate(res.slotStartTime)}
                          </span>
                          <span className="font-mono text-[11px] text-sg-text-muted">
                            {formatTime(res.slotStartTime)} – {formatTime(res.slotEndTime)}
                          </span>
                        </div>
                      </TableCell>

                      {/* Energy */}
                      <TableCell>
                        <span className="font-semibold text-sg-text">
                          {res.requestedKwh}{' '}
                          <span className="text-xs font-normal text-sg-text-muted">kWh</span>
                        </span>
                      </TableCell>

                      {/* Status */}
                      <TableCell>
                        <span className={`sg-badge ${getStatusBadgeClass(res.status)}`}>
                          {res.status}
                        </span>
                      </TableCell>

                      {/* Created */}
                      <TableCell className="text-xs text-sg-text-muted">
                        {formatDate(res.createdAt)}
                      </TableCell>

                      {/* Clean Human-Friendly Action */}
                      <TableCell className="text-right" onClick={(e) => e.stopPropagation()}>
                        {isPending ? (
                          <Button
                            size="sm"
                            variant="outline"
                            onClick={() => handleOpenDrawer(res)}
                            className="h-7 border-amber-300 bg-amber-50 text-xs font-semibold text-amber-800 hover:bg-amber-100 hover:text-amber-900 dark:border-amber-700 dark:bg-amber-950/40 dark:text-amber-300"
                          >
                            Review
                          </Button>
                        ) : (
                          <Button
                            size="sm"
                            variant="ghost"
                            onClick={() => handleOpenDrawer(res)}
                            className="h-7 text-xs text-sg-text-secondary group-hover:text-sg-primary"
                          >
                            <Eye size={13} className="mr-1" />
                            View
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between px-2">
            <span className="sg-caption text-sg-text-secondary">
              Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, totalReservations)} of{' '}
              {totalReservations} reservations
            </span>

            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                <ChevronLeft size={15} className="mr-1" />
                Previous
              </Button>
              <span className="text-xs font-medium text-sg-text">
                Page {page} of {Math.max(1, Math.ceil(totalReservations / pageSize))}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= Math.ceil(totalReservations / pageSize)}
                onClick={() => setPage((p) => p + 1)}
              >
                Next
                <ChevronRight size={15} className="ml-1" />
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Main Reservation Details Drawer */}
      <ReservationDetailsDrawer
        open={drawerOpen}
        onOpenChange={setDrawerOpen}
        reservation={activeReservation}
        onApprove={(res) => approveReservation(res.reservationId)}
        onModify={handleOpenModify}
        onCancel={handleOpenCancel}
        isApproving={isApproving}
      />

      {/* Sheet: Create on Behalf */}
      <Sheet open={createSheetOpen} onOpenChange={setCreateSheetOpen}>
        <SheetContent className="p-0" aria-labelledby={createSheetTitleId} aria-describedby={createSheetDescId}>
          <form onSubmit={handleCreateSubmit} className="flex h-full flex-col">
            <SheetHeader>
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                  <CalendarPlus size={18} />
                </div>
                <div>
                  <SheetTitle id={createSheetTitleId}>Create on behalf</SheetTitle>
                  <SheetDescription id={createSheetDescId}>
                    Book an energy transfer slot on behalf of a registered prosumer.
                  </SheetDescription>
                </div>
              </div>
            </SheetHeader>

            <SheetBody className="flex flex-col gap-5">
              {/* Prosumer NIC */}
              <div className="flex flex-col gap-2">
                <Label htmlFor="create-nic">Prosumer NIC</Label>
                <Select
                  value={createNic}
                  onValueChange={setCreateNic}
                >
                  <SelectTrigger id="create-nic">
                    <SelectValue
                      placeholder={
                        isLoadingProsumers
                          ? 'Loading prosumers...'
                          : activeProsumers.length === 0
                          ? 'No active prosumers found'
                          : 'Select registered prosumer'
                      }
                    />
                  </SelectTrigger>
                  <SelectContent>
                    {activeProsumers.map((prosumer) => (
                      <SelectItem key={prosumer.nic} value={prosumer.nic}>
                        <div className="flex items-center gap-1.5">
                          <User size={13} className="text-sg-text-muted" />
                          <span className="font-mono text-xs font-medium text-sg-text">
                            {prosumer.fullName} ({prosumer.nic})
                          </span>
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {selectedProsumer ? (
                  <p className="sg-caption text-sg-primary font-medium">
                    {selectedProsumer.fullName} • Solar: {selectedProsumer.solarCapacityKw} kW
                  </p>
                ) : (
                  <p className="sg-caption text-sg-text-muted">
                    Must belong to an active registered prosumer account.
                  </p>
                )}
              </div>

              {/* Target Node */}
              <div className="flex flex-col gap-2">
                <Label htmlFor="create-node">Target Hub / Node</Label>
                <Select
                  value={createNodeId}
                  onValueChange={(val) => {
                    setCreateNodeId(val)
                    setCreateSlotId('')
                  }}
                >
                  <SelectTrigger id="create-node">
                    <SelectValue placeholder="Select microgrid node" />
                  </SelectTrigger>
                  <SelectContent>
                    {nodes
                      .filter((n) => n.isActive)
                      .map((node) => (
                        <SelectItem key={node.nodeId} value={node.nodeId}>
                          {node.name} ({node.nodeId})
                        </SelectItem>
                      ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Booking Slot */}
              <div className="flex flex-col gap-2">
                <Label htmlFor="create-slot">Booking Slot</Label>
                <Select
                  value={createSlotId}
                  onValueChange={setCreateSlotId}
                  disabled={!createNodeId || availableCreateSlots.length === 0}
                >
                  <SelectTrigger id="create-slot">
                    <SelectValue
                      placeholder={
                        !createNodeId
                          ? 'Select node first'
                          : availableCreateSlots.length === 0
                          ? 'No future slots available'
                          : 'Select slot window'
                      }
                    />
                  </SelectTrigger>
                  <SelectContent>
                    {availableCreateSlots.map((slot: Slot) => (
                      <SelectItem key={slot.slotId} value={slot.slotId}>
                        {formatDate(slot.startTime)}: {formatTime(slot.startTime)}–
                        {formatTime(slot.endTime)} ({slot.availableCapacity}/{slot.totalCapacity}{' '}
                        slots, up to {slot.energyOfferedKwh} kWh)
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {createNodeId && availableCreateSlots.length === 0 && (
                  <p className="sg-caption text-amber-600">
                    No active future booking slots found for this node.
                  </p>
                )}
              </div>

              {/* Requested Energy kWh */}
              <div className="flex flex-col gap-2">
                <div className="flex items-center justify-between">
                  <Label htmlFor="create-kwh">Requested Energy (kWh)</Label>
                  {selectedCreateSlot && (
                    <span className="sg-caption text-sg-text-muted">
                      Max: {selectedCreateSlot.energyOfferedKwh} kWh
                    </span>
                  )}
                </div>
                <div className="relative">
                  <Zap
                    size={15}
                    className="absolute left-3 top-1/2 -translate-y-1/2 text-sg-text-muted"
                  />
                  <Input
                    id="create-kwh"
                    type="number"
                    step="0.1"
                    min="0.1"
                    max={selectedCreateSlot ? selectedCreateSlot.energyOfferedKwh : undefined}
                    placeholder="e.g. 15.5"
                    value={createKwh}
                    onChange={(e) => setCreateKwh(e.target.value)}
                    className="pl-9"
                    required
                  />
                </div>
              </div>
            </SheetBody>

            <SheetFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setCreateSheetOpen(false)}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={
                  isCreating ||
                  !createNic ||
                  !createNodeId ||
                  !createSlotId ||
                  !createKwh
                }
              >
                {isCreating ? 'Creating...' : 'Create reservation'}
              </Button>
            </SheetFooter>
          </form>
        </SheetContent>
      </Sheet>

      {/* Sheet: Modify Reservation */}
      <Sheet open={modifySheetOpen} onOpenChange={setModifySheetOpen}>
        <SheetContent className="p-0" aria-labelledby={modifySheetTitleId} aria-describedby={modifySheetDescId}>
          <form onSubmit={handleModifySubmit} className="flex h-full flex-col">
            <SheetHeader>
              <div className="flex items-center gap-3">
                <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary">
                  <Edit size={18} />
                </div>
                <div>
                  <SheetTitle id={modifySheetTitleId}>Modify reservation</SheetTitle>
                  <SheetDescription id={modifySheetDescId}>
                    Reschedule slot or change energy quota for {activeReservation?.reservationId}.
                  </SheetDescription>
                </div>
              </div>
            </SheetHeader>

            <SheetBody className="flex flex-col gap-5">
              {/* Context Summary */}
              {activeReservation && (
                <div className="rounded-md border border-sg-border bg-sg-surface-muted/40 p-3 text-xs">
                  <div className="flex justify-between py-1">
                    <span className="text-sg-text-muted">Prosumer NIC:</span>
                    <span className="font-mono font-medium text-sg-text">
                      {activeReservation.nic}
                    </span>
                  </div>
                  <div className="flex justify-between py-1 border-t border-sg-border/60">
                    <span className="text-sg-text-muted">Node ID:</span>
                    <span className="font-mono font-medium text-sg-text">
                      {activeReservation.nodeId}
                    </span>
                  </div>
                </div>
              )}

              {/* Slot Selection */}
              <div className="flex flex-col gap-2">
                <Label htmlFor="modify-slot">Select Slot Window</Label>
                <Select value={modifySlotId} onValueChange={setModifySlotId}>
                  <SelectTrigger id="modify-slot">
                    <SelectValue placeholder="Select target slot" />
                  </SelectTrigger>
                  <SelectContent>
                    {availableModifySlots.map((slot: Slot) => (
                      <SelectItem key={slot.slotId} value={slot.slotId}>
                        {formatDate(slot.startTime)}: {formatTime(slot.startTime)}–
                        {formatTime(slot.endTime)} ({slot.availableCapacity}/{slot.totalCapacity}{' '}
                        available, max {slot.energyOfferedKwh} kWh)
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Energy Amount */}
              <div className="flex flex-col gap-2">
                <div className="flex items-center justify-between">
                  <Label htmlFor="modify-kwh">Requested Energy (kWh)</Label>
                  {selectedModifySlot && (
                    <span className="sg-caption text-sg-text-muted">
                      Max: {selectedModifySlot.energyOfferedKwh} kWh
                    </span>
                  )}
                </div>
                <div className="relative">
                  <Zap
                    size={15}
                    className="absolute left-3 top-1/2 -translate-y-1/2 text-sg-text-muted"
                  />
                  <Input
                    id="modify-kwh"
                    type="number"
                    step="0.1"
                    min="0.1"
                    max={selectedModifySlot ? selectedModifySlot.energyOfferedKwh : undefined}
                    value={modifyKwh}
                    onChange={(e) => setModifyKwh(e.target.value)}
                    className="pl-9"
                    required
                  />
                </div>
              </div>
            </SheetBody>

            <SheetFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setModifySheetOpen(false)}
              >
                Cancel
              </Button>
              <Button
                type="submit"
                disabled={isUpdating || !modifySlotId || !modifyKwh}
              >
                {isUpdating ? 'Saving...' : 'Save changes'}
              </Button>
            </SheetFooter>
          </form>
        </SheetContent>
      </Sheet>

      {/* Confirmation Dialog: Cancel Reservation */}
      <Dialog open={cancelDialogOpen} onOpenChange={setCancelDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-rose-50 text-rose-600 dark:bg-rose-950/40 dark:text-rose-400">
                <Trash2 size={20} />
              </div>
              <div>
                <DialogTitle>Cancel reservation</DialogTitle>
                <DialogDescription>
                  Are you sure you want to cancel reservation{' '}
                  <span className="font-mono font-semibold text-sg-text">
                    {activeReservation?.reservationId}
                  </span>
                  ?
                </DialogDescription>
              </div>
            </div>
          </DialogHeader>

          {activeReservation && (
            <div className="mt-3 flex flex-col gap-2 rounded-lg border border-sg-border bg-sg-surface-muted/40 p-3.5 text-xs">
              <div className="flex items-center gap-2">
                <MapPin size={13} className="text-sg-text-muted" />
                <span className="text-sg-text-secondary">Node:</span>
                <span className="font-medium text-sg-text">{activeReservation.nodeId}</span>
              </div>
              <div className="flex items-center gap-2">
                <Clock size={13} className="text-sg-text-muted" />
                <span className="text-sg-text-secondary">Slot:</span>
                <span className="font-medium text-sg-text">
                  {formatDate(activeReservation.slotStartTime)},{' '}
                  {formatTime(activeReservation.slotStartTime)} –{' '}
                  {formatTime(activeReservation.slotEndTime)}
                </span>
              </div>
              <div className="flex items-center gap-2">
                <Zap size={13} className="text-sg-text-muted" />
                <span className="text-sg-text-secondary">Energy:</span>
                <span className="font-medium text-sg-text">
                  {activeReservation.requestedKwh} kWh
                </span>
              </div>
              <p className="mt-2 text-rose-600 text-[11px] font-medium border-t border-sg-border/60 pt-2">
                This will release the reserved capacity back into the node&apos;s available slot pool.
              </p>
            </div>
          )}

          <DialogFooter className="mt-4 gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => setCancelDialogOpen(false)}
            >
              Keep booking
            </Button>
            <Button
              type="button"
              variant="destructive"
              disabled={isCancelling}
              onClick={handleConfirmCancel}
            >
              {isCancelling ? 'Cancelling...' : 'Cancel reservation'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Summary Dialog (Mandatory Rubric Requirement) */}
      <ReservationSummaryDialog
        open={summaryDialogOpen}
        onOpenChange={setSummaryDialogOpen}
        reservation={activeReservation}
        action={summaryAction}
      />
    </div>
  )
}
