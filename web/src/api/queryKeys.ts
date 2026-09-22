import type { NodeListParams } from '../types/NodeTypes'
import type { SlotListParams } from '../types/SlotTypes'

const AUTH = 'auth'
const NODES = 'nodes'
const SLOTS = 'slots'

export const authQueryKeys = {
  CURRENT_USER: [AUTH, 'me'],
}

export const nodeQueryKeys = {
  ALL: [NODES],
  LIST: (params?: NodeListParams) => [NODES, 'list', params ?? {}],
}

export const slotQueryKeys = {
  BY_NODE: (nodeId: string) => [SLOTS, 'list', nodeId],
  LIST: (nodeId: string, params?: SlotListParams) => [SLOTS, 'list', nodeId, params ?? {}],
}
