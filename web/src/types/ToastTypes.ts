export type ToastVariant = 'success' | 'warning' | 'danger' | 'info'

export interface ToastInput {
  variant: ToastVariant
  title: string
  message?: string
}

export interface Toast extends ToastInput {
  id: number
}

export interface ToastContextType {
  showToast: (toast: ToastInput) => void
}
