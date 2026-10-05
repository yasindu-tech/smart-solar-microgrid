import { useQuery, type UseQueryResult } from '@tanstack/react-query'
import apiClient from './client'
import { ApiRequestError } from './types'
import { dashboardEndpoints } from './endpoints'
import { dashboardQueryKeys } from './queryKeys'
import type { OperatorDashboard, OperatorDashboardParams } from '../types/DashboardTypes'

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

const fetchOperatorDashboard = async (
  params?: OperatorDashboardParams,
): Promise<OperatorDashboard> => {
  const response = await apiClient.get(dashboardEndpoints.OPERATOR, { params })
  return response?.data
}

// Counts are scoped server-side to the caller's assigned nodes; the client never recounts them.
export const useOperatorDashboard = (
  params?: OperatorDashboardParams,
): UseQueryResult<OperatorDashboard, ApiRequestError> =>
  useQuery({
    queryKey: dashboardQueryKeys.OPERATOR(params),
    queryFn: () => fetchOperatorDashboard(params),
    staleTime: 10_000,
  })
