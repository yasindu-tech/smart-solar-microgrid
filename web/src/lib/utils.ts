import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"
import type { ReservationStatus } from '../types/ReservationTypes'
import { MS_PER_HOUR } from './constants'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function getStatusBadgeClass(status?: ReservationStatus) {
  switch (status) {
    case 'Approved':
      return 'sg-badge-success'
    case 'Pending':
      return 'sg-badge-warning'
    case 'Cancelled':
      return 'sg-badge-danger'
    case 'Completed':
      return 'sg-badge-info'
    case 'Expired':
    default:
      return 'sg-badge-neutral'
  }
}

export function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })
}

export function formatTime(iso: string) {
  return new Date(iso).toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function formatSlotDateTime(iso?: string) {
  if (!iso) return '-'
  const d = new Date(iso)
  return d.toLocaleDateString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })
}

export function formatTimeWindow(startIso?: string, endIso?: string) {
  if (!startIso || !endIso) return '-'
  const start = new Date(startIso).toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  })
  const end = new Date(endIso).toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  })
  return `${start} – ${end}`
}

export function formatFullDate(iso?: string) {
  if (!iso) return '-'
  return new Date(iso).toLocaleDateString(undefined, {
    weekday: 'long',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })
}

export function formatFullDateTime(iso?: string) {
  if (!iso) return '-'
  return new Date(iso).toLocaleString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function isWithinNoticeWindow(slotStartTimeIso?: string, minHours = 12): boolean {
  if (!slotStartTimeIso) return false
  const slotStart = new Date(slotStartTimeIso).getTime()
  const now = Date.now()
  const diffHours = (slotStart - now) / MS_PER_HOUR
  return diffHours >= minHours
}

