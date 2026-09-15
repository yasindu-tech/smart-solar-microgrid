/**
 * File        : SolarLoader.tsx
 * Project     : Smart Solar Microgrid Trading System - Web client
 * Description : Shared branded loading spinner. Per docs/architecture/
 *               ui-design-system.md sections 6 and 15 ("never create a new
 *               loading animation for a single page"), this is THE loading
 *               component for the web client - every API-driven screen that
 *               needs a spinner (not a full skeleton) imports this instead of
 *               inventing its own. Colors come from base.css tokens (no
 *               hardcoded hex), per section 2 of that same document.
 * Author      : <member name>
 */

import React from 'react'

interface SolarLoaderProps extends React.HTMLAttributes<HTMLDivElement> {
  /** Width & height of the loader (number = px, or any CSS unit string) */
  size?: number | string
  /** Show text under the spinner */
  showLabel?: boolean
  /** Custom label text */
  label?: string
  /** Path to the center icon image. Defaults to the SunGrid logo. */
  iconSrc?: string
}

/**
 * SolarLoader - the shared branded spinner.
 * A two-tone ring (primary/secondary tokens) spins around the SunGrid mark.
 *
 * @example
 *   <SolarLoader />
 *   <SolarLoader size={64} />
 *   <SolarLoader size={120} showLabel />
 *   <SolarLoader size="5rem" label="Loading reservations..." />
 */
export default function SolarLoader({
  size = 96,
  showLabel = false,
  label = 'Loading...',
  iconSrc = '/logo.png',
  className = '',
  ...rest
}: SolarLoaderProps) {
  // support either a bare number (px) or a CSS length string
  const dimension = typeof size === 'number' ? `${size}px` : size

  return (
    <div
      className={`sg-solar-loader ${className}`}
      role="status"
      aria-live="polite"
      style={{ display: 'inline-flex', flexDirection: 'column', alignItems: 'center', gap: 'var(--sg-space-3)' }}
      {...rest}
    >
      <div style={{ position: 'relative', width: dimension, height: dimension }}>
        {/* Spinning ring - uses design-system tokens, not literal hex values */}
        <div className="sg-solar-loader-ring" />

        {/* Center mark */}
        <img
          src={iconSrc}
          alt=""
          style={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            width: '58%',
            height: '58%',
            transform: 'translate(-50%, -50%)',
            objectFit: 'contain',
          }}
        />
      </div>

      {showLabel && <span className="sg-caption">{label}</span>}
      {/* Screen-reader fallback text even when the visible label is off */}
      {!showLabel && <span className="sg-sr-only">{label}</span>}
    </div>
  )
}
