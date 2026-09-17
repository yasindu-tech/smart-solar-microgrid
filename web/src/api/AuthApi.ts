import {
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
  type UseQueryResult,
} from '@tanstack/react-query'
import type { AxiosError } from 'axios'
import apiClient, { session } from './client'
import { authEndpoints } from './endpoints'
import { authQueryKeys } from './queryKeys'
import type { ApiError } from '../types/CommonTypes'
import type { CurrentUser, LoginRequest, LoginResponse } from '../types/AuthTypes'

const login = async (credentials: LoginRequest): Promise<LoginResponse> => {
  const response = await apiClient.post(authEndpoints.LOGIN, credentials)
  return response?.data
}

export const useLogin = (
  onSuccess?: (data: LoginResponse) => void,
  onError?: (error: AxiosError<ApiError>) => void,
): UseMutationResult<LoginResponse, AxiosError<ApiError>, LoginRequest> => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: login,
    onSuccess: (data) => {
      session.setToken(data.token)
      queryClient.invalidateQueries({ queryKey: authQueryKeys.CURRENT_USER })
      onSuccess?.(data)
    },
    onError,
  })
}

const logout = async (): Promise<void> => {
  await apiClient.post(authEndpoints.LOGOUT)
}

export const useLogout = (
  onSuccess?: () => void,
  onError?: (error: AxiosError<ApiError>) => void,
): UseMutationResult<void, AxiosError<ApiError>, void> => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: logout,
    // Cleared locally either way - the user's intent is to end the session
    // on this device regardless of what the server says.
    onSettled: () => {
      session.clearToken()
      queryClient.removeQueries({ queryKey: authQueryKeys.CURRENT_USER })
    },
    onSuccess,
    onError,
  })
}

const fetchCurrentUser = async (): Promise<CurrentUser> => {
  const response = await apiClient.get(authEndpoints.ME)
  return response?.data
}

export const useCurrentUser = (
  enabled?: boolean,
): UseQueryResult<CurrentUser, AxiosError<ApiError>> =>
  useQuery({
    queryKey: authQueryKeys.CURRENT_USER,
    queryFn: fetchCurrentUser,
    enabled: enabled ?? session.getToken() !== null,
    refetchOnWindowFocus: false,
  })
