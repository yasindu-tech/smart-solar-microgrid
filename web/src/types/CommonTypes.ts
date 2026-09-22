/** The error envelope every non-2xx response from the API uses (api-contract.md section 9). */
export interface ApiError {
  code: string
  message: string
  field?: string
  traceId?: string
}

/** The paging envelope every list endpoint returns. */
export interface PagedResponse<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}
