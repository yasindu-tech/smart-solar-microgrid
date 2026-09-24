import type { NodeListParams } from '../types/NodeTypes'
import type { SlotListParams } from '../types/SlotTypes'
import type { ReservationListParams } from '../types/ReservationTypes'

const AUTH = 'auth'
const NODES = 'nodes'
const SLOTS = 'slots'
const RESERVATIONS = 'reservations'

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

export const reservationQueryKeys = {
  ALL: [RESERVATIONS],
  LIST: (params?: ReservationListParams) => [RESERVATIONS, 'list', params ?? {}],
  DETAIL: (id: string) => [RESERVATIONS, 'detail', id],
}

const DASHBOARD = 'dashboard'
const PROSUMERS = 'prosumers'

export const dashboardQueryKeys = {
  BACKOFFICE: [DASHBOARD, 'backoffice'],
  PROSUMER: [DASHBOARD, 'prosumer'],
}

export const prosumerQueryKeys = {
  DETAIL: (nic: string) => [PROSUMERS, 'detail', nic],
}


