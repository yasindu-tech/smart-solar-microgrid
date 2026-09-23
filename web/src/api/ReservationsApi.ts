import {
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
  type UseQueryResult,
} from '@tanstack/react-query'
import apiClient from './client'
import { ApiRequestError } from './types'
import { reservationEndpoints } from './endpoints'
import { reservationQueryKeys, slotQueryKeys } from './queryKeys'
import type { PagedResponse } from '../types/CommonTypes'
import type {
  CreateReservationOnBehalfRequest,
  Reservation,
  ReservationListParams,
  UpdateReservationRequest,
  UseApproveReservationOptions,
  UseCancelReservationOptions,
  UseCreateReservationOptions,
  UseUpdateReservationOptions,
} from '../types/ReservationTypes'

const fetchReservations = async (
  params?: ReservationListParams,
): Promise<PagedResponse<Reservation>> => {
  const response = await apiClient.get(reservationEndpoints.BASE, { params })
  return response?.data
}

export const useReservations = (
  params?: ReservationListParams,
): UseQueryResult<PagedResponse<Reservation>, ApiRequestError> =>
  useQuery({
    queryKey: reservationQueryKeys.LIST(params),
    queryFn: () => fetchReservations(params),
  })

const fetchReservation = async (reservationId: string): Promise<Reservation> => {
  const response = await apiClient.get(reservationEndpoints.BY_ID(reservationId))
  return response?.data
}

export const useReservation = (
  reservationId: string,
): UseQueryResult<Reservation, ApiRequestError> =>
  useQuery({
    queryKey: reservationQueryKeys.DETAIL(reservationId),
    queryFn: () => fetchReservation(reservationId),
    enabled: Boolean(reservationId),
  })

const createReservationOnBehalf = async (
  request: CreateReservationOnBehalfRequest,
): Promise<Reservation> => {
  const response = await apiClient.post(reservationEndpoints.ON_BEHALF, request)
  return response?.data
}

export const useCreateReservationOnBehalf = ({
  onSuccess,
  onError,
}: UseCreateReservationOptions = {}): UseMutationResult<
  Reservation,
  ApiRequestError,
  CreateReservationOnBehalfRequest
> => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createReservationOnBehalf,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: reservationQueryKeys.ALL })
      queryClient.invalidateQueries({ queryKey: slotQueryKeys.BY_NODE(data.nodeId) })
      onSuccess?.(data)
    },
    onError,
  })
}

const updateReservation = async (
  reservationId: string,
  request: UpdateReservationRequest,
): Promise<Reservation> => {
  const response = await apiClient.put(reservationEndpoints.BY_ID(reservationId), request)
  return response?.data
}

export const useUpdateReservation = ({
  onSuccess,
  onError,
}: UseUpdateReservationOptions = {}): UseMutationResult<
  Reservation,
  ApiRequestError,
  { reservationId: string; request: UpdateReservationRequest }
> => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ reservationId, request }) => updateReservation(reservationId, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: reservationQueryKeys.ALL })
      queryClient.invalidateQueries({ queryKey: slotQueryKeys.BY_NODE(data.nodeId) })
      onSuccess?.(data)
    },
    onError,
  })
}

const approveReservation = async (reservationId: string): Promise<Reservation> => {
  const response = await apiClient.patch(reservationEndpoints.APPROVE(reservationId))
  return response?.data
}

export const useApproveReservation = ({
  onSuccess,
  onError,
}: UseApproveReservationOptions = {}): UseMutationResult<Reservation, ApiRequestError, string> => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: approveReservation,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: reservationQueryKeys.ALL })
      queryClient.invalidateQueries({ queryKey: slotQueryKeys.BY_NODE(data.nodeId) })
      onSuccess?.(data)
    },
    onError,
  })
}

const cancelReservation = async (reservationId: string): Promise<Reservation> => {
  const response = await apiClient.patch(reservationEndpoints.CANCEL(reservationId))
  return response?.data
}

export const useCancelReservation = ({
  onSuccess,
  onError,
}: UseCancelReservationOptions = {}): UseMutationResult<Reservation, ApiRequestError, string> => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: cancelReservation,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: reservationQueryKeys.ALL })
      queryClient.invalidateQueries({ queryKey: slotQueryKeys.BY_NODE(data.nodeId) })
      onSuccess?.(data)
    },
    onError,
  })
}
