/** AC or DC microgrid hub (data-model.md 1.3). */
export type NodeType = 'AC' | 'DC'

/** One operating window in a node's weekly schedule, as clients send/receive it. */
export interface ScheduleWindow {
  dayOfWeek: string
  openTime: string
  closeTime: string
}

/** GPS position and address for a node. */
export interface NodeLocation {
  latitude: number
  longitude: number
  addressLine: string
}

/** What every node endpoint returns (api-contract.md section 4). */
export interface Node {
  nodeId: string
  name: string
  location: NodeLocation
  capacityKwh: number
  batterySlotCount: number
  type: NodeType
  schedule: ScheduleWindow[]
  isActive: boolean
  createdBy: string
  createdAt: string
  updatedAt: string
}

/** POST /api/nodes body. nodeId is server-generated, never supplied. */
export interface CreateNodeRequest {
  name: string
  location: NodeLocation
  capacityKwh: number
  batterySlotCount: number
  type: NodeType
  schedule: ScheduleWindow[]
}

/** PUT /api/nodes/{nodeId} body. nodeId and isActive are not editable here. */
export interface UpdateNodeRequest {
  name: string
  location: NodeLocation
  capacityKwh: number
  batterySlotCount: number
  type: NodeType
  schedule: ScheduleWindow[]
}

/** GET /api/nodes query params. */
export interface NodeListParams {
  active?: boolean
  page?: number
  pageSize?: number
}
