import { createContext, useCallback, useState } from 'react'
import type { ReactNode } from 'react'
import { X } from 'lucide-react'
import { TOAST_AUTO_DISMISS_MS, TOAST_ICON_BY_VARIANT, TOAST_ICON_COLOR_BY_VARIANT } from './constants'
import type { Toast, ToastContextType, ToastInput } from '../types/ToastTypes'

export const ToastContext = createContext<ToastContextType | undefined>(undefined)

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])

  const dismissToast = useCallback((id: number) => {
    setToasts((prev) => prev.filter((t) => t.id !== id))
  }, [])

  const showToast = useCallback(
    ({ variant, title, message }: ToastInput) => {
      const id = Date.now() + Math.random()
      setToasts((prev) => [...prev, { id, variant, title, message }])
      setTimeout(() => dismissToast(id), TOAST_AUTO_DISMISS_MS)
    },
    [dismissToast],
  )

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}

      <div className="sg-toast-container">
        {toasts.map((toast) => {
          const Icon = TOAST_ICON_BY_VARIANT[toast.variant]
          return (
            <div key={toast.id} className={`sg-toast sg-toast-${toast.variant}`} role="alert">
              <Icon size={18} className={`mt-0.5 shrink-0 ${TOAST_ICON_COLOR_BY_VARIANT[toast.variant]}`} />
              <div className="flex-1">
                <p className="sg-toast-title">{toast.title}</p>
                {toast.message && <p className="sg-toast-message">{toast.message}</p>}
              </div>
              <button
                type="button"
                onClick={() => dismissToast(toast.id)}
                className="text-sg-text-muted hover:text-sg-text"
                aria-label="Dismiss notification"
              >
                <X size={14} />
              </button>
            </div>
          )
        })}
      </div>
    </ToastContext.Provider>
  )
}
