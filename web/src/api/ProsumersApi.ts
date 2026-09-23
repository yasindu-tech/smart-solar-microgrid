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
