import type { NodeListParams } from '../types/NodeTypes'

const AUTH = 'auth'
const NODES = 'nodes'

export const authQueryKeys = {
  CURRENT_USER: [AUTH, 'me'],
}

export const nodeQueryKeys = {
  ALL: [NODES],
  LIST: (params?: NodeListParams) => [NODES, 'list', params ?? {}],
}
