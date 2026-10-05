/**
 * File        : MetricCard.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Shared dashboard metric tile (ui-design-system.md 6, 20). Displays
 *               one API-supplied count; it never computes a value itself (REQ-VIEW AC-13).
 */

import { Link } from 'react-router-dom'
import { ArrowRight, type LucideIcon } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card'

export interface MetricCardProps {
  label: string
  value: number
  icon: LucideIcon
  href: string
  hint?: string
}

// Renders a clickable metric tile that drills into the list behind the number.
export default function MetricCard({ label, value, icon: Icon, href, hint }: MetricCardProps) {
  return (
    <Link to={href} className="group block transition-transform focus:outline-none">
      <Card className="h-full border border-sg-border bg-sg-surface transition-all group-hover:border-sg-primary/60 group-hover:shadow-sm">
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium text-sg-text-secondary">{label}</CardTitle>
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary transition-colors group-hover:bg-sg-primary group-hover:text-sg-white">
            <Icon size={16} />
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex items-baseline justify-between">
            <p className="text-3xl font-bold tracking-tight text-sg-text">{value}</p>
            <ArrowRight
              size={15}
              className="text-sg-text-muted opacity-0 transition-all group-hover:translate-x-0.5 group-hover:text-sg-primary group-hover:opacity-100"
            />
          </div>
          {hint && <p className="mt-1 text-xs text-sg-text-muted">{hint}</p>}
        </CardContent>
      </Card>
    </Link>
  )
}
