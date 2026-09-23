import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import apiClient from './client'
import { ApiRequestError } from './types'
import { prosumerEndpoints } from './endpoints'

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

const fetchProsumerByNic = async (nic: string): Promise<ProsumerDetail> => {
  const response = await apiClient.get(prosumerEndpoints.BY_NIC(nic))
  return response?.data
}

export const useProsumer = (nic: string): UseQueryResult<ProsumerDetail, ApiRequestError> =>
  useQuery({
    queryKey: ['prosumers', 'detail', nic],
    queryFn: () => fetchProsumerByNic(nic),
    enabled: Boolean(nic && nic.trim().length > 0),
  })
