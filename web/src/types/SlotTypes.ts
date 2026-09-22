/** What every slot endpoint returns (api-contract.md section 5). */
export interface Slot {
  slotId: string
  nodeId: string
  startTime: string
  endTime: string
  totalCapacity: number
  availableCapacity: number
  energyOfferedKwh: number
  isActive: boolean
  createdAt: string
  updatedAt: string
}

/** POST /api/nodes/{nodeId}/slots body. slotId is server-generated, never supplied. */
export interface CreateSlotRequest {
  startTime: string
  endTime: string
  totalCapacity: number
  energyOfferedKwh: number
}

/** PUT /api/slots/{slotId} body. Re-validated exactly like a create (REQ-SLOT 4.3). */
export interface UpdateSlotRequest {
  startTime: string
  endTime: string
  totalCapacity: number
  energyOfferedKwh: number
}

/** PATCH /api/slots/{slotId}/availability body - the operator's daily capacity update. */
export interface UpdateSlotAvailabilityRequest {
  availableCapacity: number
}

/** GET /api/nodes/{nodeId}/slots query params. */
export interface SlotListParams {
  from?: string
  to?: string
}
