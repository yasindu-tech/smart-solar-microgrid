export interface User {
  subjectId: string
  role: string
  displayName: string
}

export interface ApiError {
  code: string
  message: string
  field?: string
  traceId?: string
}

export class ApiRequestError extends Error {
  constructor(public readonly envelope: ApiError) {
    super(envelope.message)
  }
}

export interface LoginResponse {
  token: string
  role: string
  displayName: string
  nicOrUserId: string
  expiresAt: string
}

export interface LoginCredentials {
  identifier?: string
  password?: string
}
