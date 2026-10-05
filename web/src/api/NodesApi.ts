import {
  useMutation,
  useQuery,
  useQueryClient,
  type UseMutationResult,
  type UseQueryResult,
} from '@tanstack/react-query'
import apiClient from './client'
import { ApiRequestError } from './types'
import { nodeEndpoints } from './endpoints'
import { nodeQueryKeys } from './queryKeys'
import type { PagedResponse } from '../types/CommonTypes'
import type { CreateNodeRequest, Node, NodeListParams, UseCreateNodeOptions } from '../types/NodeTypes'

const fetchNodes = async (params?: NodeListParams): Promise<PagedResponse<Node>> => {
  const response = await apiClient.get(nodeEndpoints.BASE, { params })
  return response?.data
}

export const useNodes = (
  params?: NodeListParams,
): UseQueryResult<PagedResponse<Node>, ApiRequestError> =>
  useQuery({
    queryKey: nodeQueryKeys.LIST(params),
    queryFn: () => fetchNodes(params),
  })

const createNode = async (request: CreateNodeRequest): Promise<Node> => {
  const response = await apiClient.post(nodeEndpoints.BASE, request)
  return response?.data
}

export const useCreateNode = (
  { onSuccess, onError }: UseCreateNodeOptions = {},
): UseMutationResult<Node, ApiRequestError, CreateNodeRequest> => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createNode,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: nodeQueryKeys.ALL })
      onSuccess?.(data)
    },
    onError,
  })
}
