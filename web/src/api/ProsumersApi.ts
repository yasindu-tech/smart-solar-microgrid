import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import apiClient from './client'
import { ApiRequestError } from './types'
import { prosumerEndpoints } from './endpoints'
import { prosumerQueryKeys } from './queryKeys'
import type { PagedResponse } from '../types/CommonTypes'
import type { ProsumerDetail } from '../types/ReservationTypes'

export interface UseProsumerOptions {
  enabled?: boolean
}

export interface ProsumerListParams {
  status?: string
  search?: string
  page?: number
  pageSize?: number
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

const fetchProsumers = async (params?: ProsumerListParams): Promise<PagedResponse<ProsumerDetail>> => {
  const response = await apiClient.get(prosumerEndpoints.BASE, { params })
  return response?.data
}

export const useProsumers = (
  params?: ProsumerListParams,
  options?: { enabled?: boolean },
): UseQueryResult<PagedResponse<ProsumerDetail>, ApiRequestError> =>
  useQuery({
    queryKey: prosumerQueryKeys.LIST(params as Record<string, unknown>),
    queryFn: () => fetchProsumers(params),
    enabled: options?.enabled ?? true,
  })
