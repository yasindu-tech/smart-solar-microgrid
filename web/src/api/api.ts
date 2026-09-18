import apiClient from './client'
import { LoginCredentials, LoginResponse, User } from './types'

export async function loginApi(credentials: LoginCredentials): Promise<LoginResponse> {
  const response = await apiClient.post<LoginResponse>('/auth/login', credentials)
  return response.data
}

export async function logoutApi(): Promise<void> {
  await apiClient.post('/auth/logout')
}

export async function getMeApi(): Promise<User> {
  const response = await apiClient.get<User>('/auth/me')
  return response.data
}
