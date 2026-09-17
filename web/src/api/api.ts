import { request } from './client'
import { LoginCredentials, LoginResponse, User } from './types'

export async function loginApi(credentials: LoginCredentials): Promise<LoginResponse> {
  return request<LoginResponse>('/auth/login', {
    method: 'POST',
    body: JSON.stringify(credentials),
  })
}

export async function logoutApi(): Promise<void> {
  return request<void>('/auth/logout', {
    method: 'POST',
  })
}

export async function getMeApi(): Promise<User> {
  return request<User>('/auth/me', {
    method: 'GET',
  })
}
