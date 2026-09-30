/**
 * File        : ErrorState.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Shared data-load failure panel built on .sg-error-state
 *               (ui-design-system.md 17). Shows a mapped, user-friendly message
 *               and a retry action - never a raw exception or stack trace.
 */

import { AlertTriangle, RotateCcw } from 'lucide-react'
import { Button } from '../ui/button'

export interface ErrorStateProps {
  title: string
  message: string
  onRetry?: () => void
  isRetrying?: boolean
}

// Renders the failure message with an optional retry button.
export default function ErrorState({ title, message, onRetry, isRetrying = false }: ErrorStateProps) {
  return (
    <div className="sg-error-state" role="alert">
      <div className="sg-error-icon">
        <AlertTriangle size={24} className="text-sg-danger" aria-hidden="true" />
      </div>
      <p className="sg-h4">{title}</p>
      <p className="sg-body-sm text-sg-text-secondary">{message}</p>
      {onRetry && (
        <Button variant="outline" size="sm" onClick={onRetry} disabled={isRetrying}>
          <RotateCcw size={14} />
          {isRetrying ? 'Retrying...' : 'Try again'}
        </Button>
      )}
    </div>
  )
}
