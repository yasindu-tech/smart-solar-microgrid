import {
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
  type UseQueryResult,
} from '@tanstack/react-query'
import apiClient from './client'
import { ApiRequestError } from './types'
import { slotEndpoints } from './endpoints'
import { slotQueryKeys } from './queryKeys'
import type {
  CreateSlotRequest,
  Slot,
  SlotListParams,
  UpdateSlotAvailabilityRequest,
} from '../types/SlotTypes'

const fetchSlots = async (nodeId: string, params?: SlotListParams): Promise<Slot[]> => {
  const response = await apiClient.get(slotEndpoints.BY_NODE(nodeId), { params })
  return response?.data
}

export const useSlots = (
  nodeId: string,
  params?: SlotListParams,
): UseQueryResult<Slot[], ApiRequestError> =>
  useQuery({
    queryKey: slotQueryKeys.LIST(nodeId, params),
    queryFn: () => fetchSlots(nodeId, params),
    enabled: Boolean(nodeId),
  })

const createSlot = async (nodeId: string, request: CreateSlotRequest): Promise<Slot> => {
  const response = await apiClient.post(slotEndpoints.BY_NODE(nodeId), request)
  return response?.data
}

interface UseCreateSlotOptions {
  onSuccess?: (data: Slot) => void
  onError?: (error: ApiRequestError) => void
}

export const useCreateSlot = (
  nodeId: string,
  { onSuccess, onError }: UseCreateSlotOptions = {},
): UseMutationResult<Slot, ApiRequestError, CreateSlotRequest> => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateSlotRequest) => createSlot(nodeId, request),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: slotQueryKeys.BY_NODE(nodeId) })
      onSuccess?.(data)
    },
    onError,
  })
}

const updateSlotAvailability = async (
  slotId: string,
  request: UpdateSlotAvailabilityRequest,
): Promise<Slot> => {
  const response = await apiClient.patch(slotEndpoints.AVAILABILITY(slotId), request)
  return response?.data
}

interface UpdateSlotAvailabilityVariables {
  slotId: string
  availableCapacity: number
}

interface UseUpdateSlotAvailabilityOptions {
  onSuccess?: (data: Slot) => void
  onError?: (error: ApiRequestError) => void
}

export const useUpdateSlotAvailability = (
  nodeId: string,
  { onSuccess, onError }: UseUpdateSlotAvailabilityOptions = {},
): UseMutationResult<Slot, ApiRequestError, UpdateSlotAvailabilityVariables> => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ slotId, availableCapacity }: UpdateSlotAvailabilityVariables) =>
      updateSlotAvailability(slotId, { availableCapacity }),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: slotQueryKeys.BY_NODE(nodeId) })
      onSuccess?.(data)
    },
    onError,
  })
}
