import type { ApiRequestError } from '../api/types'

export type ReservationStatus = 'Pending' | 'Approved' | 'Cancelled' | 'Completed' | 'Expired'

export interface Reservation {
  reservationId: string
  nic: string
  nodeId: string
  slotId: string
  slotStartTime: string
  slotEndTime: string
  requestedKwh: number
  status: ReservationStatus
  qrToken?: string | null
  qrIssuedAt?: string | null
  createdAt: string
  updatedAt: string
  cancelledAt?: string | null
  cancelledBy?: string | null
  completedAt?: string | null
  completedByOperatorId?: string | null
  deliveredKwh?: number | null
}

export interface PagedResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface ReservationListParams {
  status?: string
  nodeId?: string
  from?: string
  to?: string
  search?: string
  page?: number
  pageSize?: number
}

export interface CreateReservationOnBehalfRequest {
  nic: string
  nodeId: string
  slotId: string
  requestedKwh: number
}

export interface UpdateReservationRequest {
  slotId: string
  requestedKwh: number
}

export interface UseCreateReservationOptions {
  onSuccess?: (data: Reservation) => void
  onError?: (error: ApiRequestError) => void
}

export interface UseUpdateReservationOptions {
  onSuccess?: (data: Reservation) => void
  onError?: (error: ApiRequestError) => void
}

export interface UseApproveReservationOptions {
  onSuccess?: (data: Reservation) => void
  onError?: (error: ApiRequestError) => void
}

export interface UseCancelReservationOptions {
  onSuccess?: (data: Reservation) => void
  onError?: (error: ApiRequestError) => void
}
