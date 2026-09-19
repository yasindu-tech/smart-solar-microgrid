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
