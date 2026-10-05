/**
 * Google Maps JavaScript API loader.
 *
 * The Maps script is a single global that must be injected once per document,
 * so this module owns the tag and hands every caller the same promise.
 */
import { MAPS_LIBRARIES, MAPS_READY_CALLBACK, MAPS_SCRIPT_URL, MAPS_VERSION } from './constants'

/** The browser key, inlined by Vite at build time. */
export const googleMapsApiKey: string = import.meta.env.VITE_GOOGLE_MAP_API_KEY ?? ''

export const isGoogleMapsConfigured = (): boolean => googleMapsApiKey.trim().length > 0

let loaderPromise: Promise<typeof google.maps> | null = null

/**
 * Loads the Maps script once and resolves with `google.maps`. Repeat calls share
 * the first promise; a failed load clears it so a later retry can try again.
 */
export function loadGoogleMaps(): Promise<typeof google.maps> {
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
    script.src = `${MAPS_SCRIPT_URL}?${params.toString()}`
    script.async = true

    const cleanUpCallback = () => {
      delete globals[MAPS_READY_CALLBACK]
    }

    // `loading=async` assigns window.google.maps *after* the script's load
    // event, so only this callback reports readiness. Resolving on `load`
    // instead is a race that rejects on a cold cache and passes on a warm one.
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
