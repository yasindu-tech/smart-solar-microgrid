/** Prosumer account statuses. */
export type ProsumerStatus = 'PendingActivation' | 'Active' | 'Deactivated'

/** GET /prosumers/{nic} — matches backend ProsumerResponse record. */
export interface Prosumer {
  nic: string
  fullName: string
  email: string
  phone: string
  address: string
  solarCapacityKw: number
  status: ProsumerStatus
  deactivationRequestedAt: string | null
  reactivatedBy: string | null
  createdAt: string
  updatedAt: string
}

/** PUT /prosumers/{nic} body (Backoffice or self). NIC is NOT in the body. */
export interface UpdateProsumerRequest {
  fullName: string
  email: string
  phone: string
  address: string
  solarCapacityKw: number
}
