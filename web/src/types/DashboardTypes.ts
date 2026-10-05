import type { Reservation } from './ReservationTypes'

/** GET /dashboard/backoffice 200 response. */
export interface BackofficeDashboard {
  totalProsumers: number
  pendingActivations: number
  activeNodes: number
  reservationsByStatus: ReservationsByStatus
}

/**
 * GET /dashboard/operator 200 response (REQ-VIEW 2.3). Every count is computed by the API
 * and already scoped to the operator's assigned nodes (BR-11).
 */
export interface OperatorDashboard {
  pendingCount: number
  approvedFutureCount: number
  /** Slots starting within the current UTC day, soonest first. qrToken is always null here. */
  todaysReservations: Reservation[]
  /** The node ids these figures cover - all assigned nodes, or the one passed as ?nodeId=. */
  scopedNodeIds: string[]
}

/** GET /dashboard/operator query params. nodeId may only narrow the operator's own scope. */
export interface OperatorDashboardParams {
  nodeId?: string
}

/** Reservation counts grouped by status for dashboard cards. */
export interface ReservationsByStatus {
  pending: number
  approved: number
  completed: number
  cancelled: number
  expired: number
}
