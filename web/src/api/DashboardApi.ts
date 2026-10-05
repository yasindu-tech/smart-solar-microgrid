import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import apiClient from './client'
import { ApiRequestError } from './types'
import { dashboardEndpoints } from './endpoints'
import { dashboardQueryKeys } from './queryKeys'
import type { ProsumerDashboard } from '../types/DashboardTypes'

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

const fetchProsumerDashboard = async (): Promise<ProsumerDashboard> => {
  const response = await apiClient.get(dashboardEndpoints.PROSUMER)
  return response?.data
}

/**
 * The authenticated prosumer's own dashboard. The API scopes it to the NIC in
 * the token (BR-02), so no identifier is passed from the client.
 */
export const useProsumerDashboard = (): UseQueryResult<
  ProsumerDashboard,
  ApiRequestError
> =>
  useQuery({
    queryKey: dashboardQueryKeys.PROSUMER,
    queryFn: fetchProsumerDashboard,
    staleTime: 10_000,
  })
