import { AlertTriangle, CheckCircle, Info, XCircle } from 'lucide-react'
import type { ToastVariant } from '../types/ToastTypes'

export const SOLAR_LOADER_SIZE_CLASSES = {
  sm: 'sg-solar-loader-sm',
  md: 'sg-solar-loader-md',
  lg: 'sg-solar-loader-lg',
} as const

export const TOAST_ICON_BY_VARIANT = {
  success: CheckCircle,
  warning: AlertTriangle,
  danger: XCircle,
  info: Info,
}

export const TOAST_ICON_COLOR_BY_VARIANT: Record<ToastVariant, string> = {
  success: 'text-sg-success',
  warning: 'text-sg-warning',
  danger: 'text-sg-danger',
  info: 'text-sg-info',
}

export const TOAST_AUTO_DISMISS_MS = 5000

export const DAYS_OF_WEEK = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']

export const MS_PER_HOUR = 1000 * 60 * 60

export const RESERVATION_ACTIONS = ['Created', 'Modified', 'Cancelled', 'Approved'] as const
export type ReservationAction = (typeof RESERVATION_ACTIONS)[number]

export const RESERVATION_STATUSES = ['Pending', 'Approved', 'Cancelled', 'Completed', 'Expired'] as const
export type ReservationStatus = (typeof RESERVATION_STATUSES)[number]

export const DEFAULT_PAGE_SIZE = 10

// Location picker: how long to wait after the last keystroke before asking
// Google for suggestions, and how many of them to show.
export const GEOCODE_DEBOUNCE_MS = 350
export const MAX_PLACE_SUGGESTIONS = 5

