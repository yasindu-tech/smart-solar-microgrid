import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import apiClient from './client'
import { ApiRequestError } from './types'
import { prosumerEndpoints } from './endpoints'
import { prosumerQueryKeys } from './queryKeys'
import type { ProsumerDetail } from '../types/ReservationTypes'

export interface UseProsumerOptions {
  enabled?: boolean
}

const fetchProsumerByNic = async (nic: string): Promise<ProsumerDetail> => {
  const response = await apiClient.get(prosumerEndpoints.BY_NIC(nic))
  return response?.data
}

export const useProsumer = (
  nic: string,
  options?: UseProsumerOptions,
): UseQueryResult<ProsumerDetail, ApiRequestError> =>
  useQuery({
    queryKey: prosumerQueryKeys.DETAIL(nic),
    queryFn: () => fetchProsumerByNic(nic),
    enabled: (options?.enabled ?? true) && Boolean(nic && nic.trim().length > 0),
  })

// --- Restored methods ---
import type { Prosumer, UpdateProsumerRequest } from '../types/ProsumerTypes'
import type { PagedResponse } from '../types/PagedResponse'
import { useInfiniteQuery } from '@tanstack/react-query'

export const fetchProsumers = async (
  params: { status?: string; search?: string; page?: number; pageSize?: number } = {}
): Promise<PagedResponse<Prosumer>> => {
  const response = await apiClient.get(prosumerEndpoints.BASE, { params })
  return response?.data
}

export const fetchPendingProsumers = async (
  params: { page?: number; pageSize?: number } = {}
): Promise<PagedResponse<Prosumer>> => {
  const response = await apiClient.get(prosumerEndpoints.PENDING, { params })
  return response?.data
}

export const updateProsumer = async (nic: string, data: UpdateProsumerRequest): Promise<Prosumer> => {
  const response = await apiClient.put(prosumerEndpoints.BY_NIC(nic), data)
  return response?.data
}

export const activateProsumer = async (nic: string): Promise<void> => {
  await apiClient.patch(prosumerEndpoints.ACTIVATE(nic))
}

export const deactivateProsumer = async (nic: string): Promise<void> => {
  await apiClient.patch(prosumerEndpoints.DEACTIVATE(nic))
}

export const usePendingProsumersInfinite = (pageSize: number) =>
  useInfiniteQuery({
    queryKey: ['prosumers', 'pending', 'infinite'],
    queryFn: ({ pageParam = 1 }) => fetchPendingProsumers({ page: pageParam as number, pageSize }),
    initialPageParam: 1,
    getNextPageParam: (lastPage, allPages) => {
      const totalPages = Math.ceil(lastPage.total / pageSize)
      const nextPage = allPages.length + 1
      return nextPage <= totalPages ? nextPage : undefined
    },
  })
