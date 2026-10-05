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
