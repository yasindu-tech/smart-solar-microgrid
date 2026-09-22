import apiClient from './client'
import type { Prosumer, UpdateProsumerRequest } from '../types/ProsumerTypes'
import type { PagedResponse } from '../types/PagedResponse'

/** GET /prosumers — paginated list with optional status filter and search. */
export async function fetchProsumers(
  params: { status?: string; search?: string; page?: number; pageSize?: number } = {}
): Promise<PagedResponse<Prosumer>> {
  const response = await apiClient.get<PagedResponse<Prosumer>>('/prosumers', { params })
  return response.data
}

/** GET /prosumers/pending — the pending-activation queue (Backoffice only). */
export async function fetchPendingProsumers(
  params: { page?: number; pageSize?: number } = {}
): Promise<PagedResponse<Prosumer>> {
  const response = await apiClient.get<PagedResponse<Prosumer>>('/prosumers/pending', { params })
  return response.data
}

/** GET /prosumers/{nic}. */
export async function fetchProsumerByNic(nic: string): Promise<Prosumer> {
  const response = await apiClient.get<Prosumer>(`/prosumers/${nic}`)
  return response.data
}

/** PUT /prosumers/{nic} — update profile (Backoffice or self). */
export async function updateProsumer(nic: string, data: UpdateProsumerRequest): Promise<Prosumer> {
  const response = await apiClient.put<Prosumer>(`/prosumers/${nic}`, data)
  return response.data
}

/** PATCH /prosumers/{nic}/activate — approve or reactivate (Backoffice only, BR-03). */
export async function activateProsumer(nic: string): Promise<void> {
  await apiClient.patch(`/prosumers/${nic}/activate`)
}

/** PATCH /prosumers/{nic}/deactivate — deactivate account (BR-04 cascade). */
export async function deactivateProsumer(nic: string): Promise<void> {
  await apiClient.patch(`/prosumers/${nic}/deactivate`)
}
