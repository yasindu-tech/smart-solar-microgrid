export const authEndpoints = {
  LOGIN: '/auth/login',
  LOGOUT: '/auth/logout',
  ME: '/auth/me',
}

export const nodeEndpoints = {
  BASE: '/nodes',
}

export const slotEndpoints = {
  BY_NODE: (nodeId: string) => `/nodes/${nodeId}/slots`,
  BY_ID: (slotId: string) => `/slots/${slotId}`,
  AVAILABILITY: (slotId: string) => `/slots/${slotId}/availability`,
}
