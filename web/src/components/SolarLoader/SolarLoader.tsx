import { SOLAR_LOADER_SIZE_CLASSES } from '../../lib/constants'

export type SolarLoaderSize = keyof typeof SOLAR_LOADER_SIZE_CLASSES

export interface SolarLoaderProps {
  size?: SolarLoaderSize
  showLabel?: boolean
  label?: string
  iconSrc?: string
  className?: string
}

export default function SolarLoader({
  size = 'md',
  showLabel = false,
  label = 'Loading...',
  iconSrc = '/logo.png',
  className,
}: SolarLoaderProps) {
  const classes = ['sg-solar-loader', SOLAR_LOADER_SIZE_CLASSES[size], className]
    .filter(Boolean)
    .join(' ')

  return (
    <div className={classes} role="status" aria-live="polite">
      <div className="sg-solar-loader-figure">
        <div className="sg-solar-loader-ring" />
        <img src={iconSrc} alt="" className="sg-solar-loader-mark" />
      </div>

      {showLabel ? (
        <span className="sg-caption">{label}</span>
      ) : (
        <span className="sg-sr-only">{label}</span>
      )}
    </div>
  )
}
