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

export const reservationEndpoints = {
  BASE: '/reservations',
  ON_BEHALF: '/reservations',
  BY_ID: (id: string) => `/reservations/${id}`,
  APPROVE: (id: string) => `/reservations/${id}/approve`,
  CANCEL: (id: string) => `/reservations/${id}/cancel`,
  QR: (id: string) => `/reservations/${id}/qr`,
}

export const prosumerEndpoints = {
  BASE: '/prosumers',
  PENDING: '/prosumers/pending',
  BY_NIC: (nic: string) => `/prosumers/${nic}`,
  ACTIVATE: (nic: string) => `/prosumers/${nic}/activate`,
  DEACTIVATE: (nic: string) => `/prosumers/${nic}/deactivate`,
}

export const dashboardEndpoints = {
  BACKOFFICE: '/dashboard/backoffice',
  PROSUMER: '/dashboard/prosumer',
  OPERATOR: '/dashboard/operator',
}



