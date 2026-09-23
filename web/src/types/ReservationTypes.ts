import type { ApiRequestError } from '../api/types'
import { RESERVATION_STATUSES, type ReservationAction, type ReservationStatus } from '../lib/constants'

export { RESERVATION_STATUSES, type ReservationStatus }

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

export interface ProsumerDetail {
  nic: string
  fullName: string
  email: string
  phone: string
  address: string
  solarCapacityKw: number
  status: string
  createdAt: string
  updatedAt: string
}

export interface ReservationDetailsDrawerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  reservation: Reservation | null
  onApprove: (res: Reservation) => void
  onModify: (res: Reservation) => void
  onCancel: (res: Reservation) => void
  isApproving?: boolean
}

export interface ReservationSummaryDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  reservation: Reservation | null
  action: ReservationAction
}

