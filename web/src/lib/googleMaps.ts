/**
 * Google Maps JavaScript API loader.
 *
 * The Maps script is a single global that must be injected once per document,
 * so this module owns the tag and hands every caller the same promise. Nothing
 * here decides anything about a node - it only makes `window.google.maps`
 * available so the location picker can render a map and geocode an address.
 */

/** Libraries the location picker needs: `places` for search, `marker` for the pin. */
const MAPS_LIBRARIES = 'places,marker'

/** Pinned so a Google release cannot silently change the picker's behaviour. */
const MAPS_VERSION = 'weekly'

/** Global Google calls once the API is ready; required when `loading=async`. */
const MAPS_READY_CALLBACK = '__sunGridGoogleMapsReady'

/** The browser key, inlined by Vite at build time. */
export const googleMapsApiKey: string = import.meta.env.VITE_GOOGLE_MAP_API_KEY ?? ''

/** True when a key is configured; the picker degrades to manual entry without one. */
export const isGoogleMapsConfigured = (): boolean => googleMapsApiKey.trim().length > 0

/** Map id required by AdvancedMarkerElement; DEMO_MAP_ID needs no cloud styling set up. */
export const SUNGRID_MAP_ID = 'DEMO_MAP_ID'

/** Colombo - the grid's home city, used until the operator picks a real spot. */
export const DEFAULT_MAP_CENTER = { lat: 6.9271, lng: 79.8612 }

/** Zoom used for the default center, and the closer zoom applied once a pin exists. */
export const DEFAULT_MAP_ZOOM = 11
export const PINNED_MAP_ZOOM = 16

let loaderPromise: Promise<typeof google.maps> | null = null

/**
 * Loads the Maps script once and resolves with `google.maps`. Repeat calls share
 * the first promise; a failed load clears it so a later retry can try again.
 */
export function loadGoogleMaps(): Promise<typeof google.maps> {
  // Already loaded by an earlier call (or a previous mount) - reuse it.
  if (typeof window !== 'undefined' && window.google?.maps) {
    return Promise.resolve(window.google.maps)
  }

  if (loaderPromise) return loaderPromise

  if (!isGoogleMapsConfigured()) {
    return Promise.reject(new Error('Google Maps API key is not configured.'))
  }

  loaderPromise = new Promise<typeof google.maps>((resolve, reject) => {
    const globals = window as unknown as Record<string, unknown>

    const params = new URLSearchParams({
      key: googleMapsApiKey,
      libraries: MAPS_LIBRARIES,
      v: MAPS_VERSION,
      loading: 'async',
      callback: MAPS_READY_CALLBACK,
    })

    const script = document.createElement('script')
    script.src = `https://maps.googleapis.com/maps/api/js?${params.toString()}`
    script.async = true

    const cleanUpCallback = () => {
      delete globals[MAPS_READY_CALLBACK]
    }

    // `loading=async` assigns window.google.maps *after* the script's load
    // event, so the load event says nothing about readiness - only this
    // callback does. Resolving on `load` instead is a race that usually
    // rejects on a cold cache and succeeds on a warm one.
    globals[MAPS_READY_CALLBACK] = () => {
      cleanUpCallback()
      if (window.google?.maps) {
        resolve(window.google.maps)
      } else {
        loaderPromise = null
        reject(new Error('Google Maps loaded without exposing the maps namespace.'))
      }
    }

    script.addEventListener('error', () => {
      // Drop the cached promise so a retry re-injects the tag.
      cleanUpCallback()
      loaderPromise = null
      script.remove()
      reject(new Error('Could not load Google Maps.'))
    })

    document.head.appendChild(script)
  })

  return loaderPromise
}

/** Rounds a coordinate to ~1 m precision so the form never carries float noise. */
export function roundCoordinate(value: number): number {
  return Number(value.toFixed(6))
}

/** Formats a coordinate pair for display next to the map. */
export function formatCoordinates(latitude: number, longitude: number): string {
  return `${roundCoordinate(latitude)}, ${roundCoordinate(longitude)}`
}
