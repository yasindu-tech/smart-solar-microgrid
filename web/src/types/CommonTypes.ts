/** The error envelope every non-2xx response from the API uses (api-contract.md section 9). */
export interface ApiError {
  code: string
  message: string
  field?: string
  traceId?: string
}
