/**
 * SolarLoader size variants map to .sg-solar-loader-* in base.css, which
 * read the --sg-loader-* tokens. Those tokens already change at the md
 * breakpoint, so a variant is correct on phone and desktop without the
 * component knowing any pixel value or breakpoint.
 *
 *   sm - inline, next to a row or a field   (32px phone / 40px desktop)
 *   md - a panel, card or table body        (56px / 72px)
 *   lg - a full page or route transition    (80px / 112px)
 */
export const SOLAR_LOADER_SIZE_CLASSES = {
  sm: 'sg-solar-loader-sm',
  md: 'sg-solar-loader-md',
  lg: 'sg-solar-loader-lg',
} as const
