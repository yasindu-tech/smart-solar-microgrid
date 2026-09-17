// `as const` is recursive - one assertion here covers every nested object/array
// below, so individual exports don't need to repeat it.
const constants = {
  SOLAR_LOADER_SIZE_CLASSES: {
    sm: 'sg-solar-loader-sm',
    md: 'sg-solar-loader-md',
    lg: 'sg-solar-loader-lg',
  },
} as const

export const { SOLAR_LOADER_SIZE_CLASSES } = constants
