import apiClient from './client'
import type { WebUser, CreateUserRequest, UpdateUserRequest } from '../types/UserTypes'
import type { PagedResponse } from '../types/PagedResponse'

/** GET /users — paginated list, optional role filter. */
export async function fetchUsers(
  params: { role?: string; page?: number; pageSize?: number } = {}
): Promise<PagedResponse<WebUser>> {
  const response = await apiClient.get<PagedResponse<WebUser>>('/users', { params })
  return response.data
}

/** GET /users/{userId}. */
export async function fetchUserById(userId: string): Promise<WebUser> {
  const response = await apiClient.get<WebUser>(`/users/${userId}`)
  return response.data
}

/** POST /users — create a new Backoffice or Operator user. */
export async function createUser(data: CreateUserRequest): Promise<WebUser> {
  const response = await apiClient.post<WebUser>('/users', data)
  return response.data
}

/** PUT /users/{userId} — update user details. */
export async function updateUser(userId: string, data: UpdateUserRequest): Promise<WebUser> {
  const response = await apiClient.put<WebUser>(`/users/${userId}`, data)
  return response.data
}

/** PATCH /users/{userId}/status — activate or deactivate. */
export async function setUserStatus(userId: string, isActive: boolean): Promise<void> {
  await apiClient.patch(`/users/${userId}/status`, { isActive })
}

/** DELETE /users/{userId} — soft delete (sets isActive:false). */
export async function deleteUser(userId: string): Promise<void> {
  await apiClient.delete(`/users/${userId}`)
}
