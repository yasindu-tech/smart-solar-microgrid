

/** The three roles the API issues tokens for (api-contract.md section 1). */
export type Role = 'Backoffice' | 'Operator' | 'Prosumer'

/** POST /auth/login body. `identifier` is an email for web users, a NIC for prosumers. */
export interface LoginRequest {
  identifier: string
  password: string
}

/** POST /auth/login 200 response. */
export interface LoginResponse {
  token: string
  role: Role
  displayName: string
  nicOrUserId: string
  expiresAt: string
}

/** GET /auth/me 200 response - the currently authenticated principal. */
export interface CurrentUser {
  subjectId: string
  role: Role
  displayName: string
}
