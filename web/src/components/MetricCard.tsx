/**
 * File        : MetricCard.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Shared dashboard metric tile - one headline number, a label,
 *               an icon and an optional hint. Used by every dashboard so the
 *               backoffice, operator and prosumer screens cannot drift apart
 *               (ui-design-system.md sections 7 and 20).
 */

import { Link } from 'react-router-dom'
import { ArrowRight, type LucideIcon } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from './ui/card'

export interface MetricCardProps {
  label: string
  value: number | string
  icon: LucideIcon
  /** Optional sub-label under the number. */
  hint?: string
  /** When set the whole tile becomes a link and grows an affordance arrow. */
  href?: string
}

export default function MetricCard({ label, value, icon: Icon, hint, href }: MetricCardProps) {
  // The tile renders identically whether or not it links anywhere; only the
  // hover affordance and the wrapping element differ, so the markup is built
  // once here and wrapped below.
  const tile = (
    <Card className="h-full border border-sg-border bg-sg-surface transition-all group-hover:border-sg-primary/60 group-hover:shadow-sm">
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-sg-text-secondary">{label}</CardTitle>
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-sg-primary-soft text-sg-primary transition-colors group-hover:bg-sg-primary group-hover:text-sg-text-on-primary">
          <Icon size={16} />
        </div>
      </CardHeader>
      <CardContent>
        <div className="flex items-baseline justify-between">
          <p className="text-3xl font-bold tracking-tight text-sg-text">{value}</p>
          {href && (
            <ArrowRight
              size={15}
              className="text-sg-text-muted opacity-0 transition-all group-hover:translate-x-0.5 group-hover:text-sg-primary group-hover:opacity-100"
            />
          )}
        </div>
        {hint && <p className="mt-1 text-xs text-sg-text-muted">{hint}</p>}
      </CardContent>
    </Card>
  )

  // A non-linking tile still carries the `group` class so the icon's hover
  // treatment stays consistent across dashboards.
  if (!href) return <div className="group block">{tile}</div>

  return (
    <Link to={href} className="group block transition-transform focus:outline-none">
      {tile}
    </Link>
  )
}
