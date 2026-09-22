import type { Role } from './AuthTypes'

/** GET /users/{userId} — matches backend UserResponse record. */
export interface WebUser {
  userId: string
  fullName: string
  email: string
  role: Role
  assignedNodeIds: string[]
  isActive: boolean
  createdAt: string
  updatedAt: string
}

/** POST /users body. */
export interface CreateUserRequest {
  fullName: string
  email: string
  password: string
  role: 'Backoffice' | 'Operator'
  assignedNodeIds: string[]
}

/** PUT /users/{userId} body — password is optional on update. */
export interface UpdateUserRequest {
  fullName: string
  email: string
  password?: string
  role: 'Backoffice' | 'Operator'
  assignedNodeIds: string[]
}

/** PATCH /users/{userId}/status body. */
export interface UserStatusRequest {
  isActive: boolean
}
