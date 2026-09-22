import apiClient from './client'
import type { BackofficeDashboard } from '../types/DashboardTypes'

/** GET /dashboard/backoffice — Backoffice summary counts. */
export async function fetchBackofficeDashboard(): Promise<BackofficeDashboard> {
  const response = await apiClient.get<BackofficeDashboard>('/dashboard/backoffice')
  return response.data
}
