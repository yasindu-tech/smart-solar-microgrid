import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import apiClient from './client'
import { ApiRequestError } from './types'
import { dashboardEndpoints } from './endpoints'
import { dashboardQueryKeys } from './queryKeys'

export interface BackofficeDashboardData {
  totalProsumers: number
  pendingActivations: number
  activeNodes: number
  totalReservations: number
  reservationsByStatus: Record<string, number>
}

const fetchBackofficeDashboard = async (): Promise<BackofficeDashboardData> => {
  const response = await apiClient.get(dashboardEndpoints.BACKOFFICE)
  return response?.data
}

export const useBackofficeDashboard = (): UseQueryResult<
  BackofficeDashboardData,
  ApiRequestError
> =>
  useQuery({
    queryKey: dashboardQueryKeys.BACKOFFICE,
    queryFn: fetchBackofficeDashboard,
    staleTime: 10_000,
  })
