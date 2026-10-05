/**
 * File        : EmptyState.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Shared empty-collection placeholder built on .sg-empty
 *               (ui-design-system.md 16). Never leave a zero-row list blank.
 */

import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'

export interface EmptyStateProps {
  icon: LucideIcon
  title: string
  description?: string
  action?: ReactNode
}

// Renders an icon, a title, an explanation and an optional call to action.
export default function EmptyState({ icon: Icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="sg-empty">
      <div className="sg-empty-icon">
        <Icon size={24} aria-hidden="true" />
      </div>
      <p className="sg-h4">{title}</p>
      {description && <p className="sg-body-sm text-sg-text-secondary">{description}</p>}
      {action}
    </div>
  )
}
