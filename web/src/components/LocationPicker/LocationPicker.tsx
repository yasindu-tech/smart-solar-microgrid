/**
 * LocationPicker - pick a node's position by searching for a place or dropping a
 * pin on a map, instead of typing raw coordinates.
 *
 * This is a UI convenience only. Latitude/longitude ranges, and every other node
 * rule, are still validated by the Web API (REQ-NODE 4.1 step 3, AC-2). The map is
 * the only way to set a position, so the caller should keep submit disabled until
 * `value.latitude`/`value.longitude` are filled - a hint, not a rule.
 */
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Crosshair, Loader2, MapPin, Search, X } from 'lucide-react'
import {
  DEFAULT_MAP_CENTER,
  DEFAULT_MAP_ZOOM,
  PINNED_MAP_ZOOM,
  SUNGRID_MAP_ID,
  formatCoordinates,
  isGoogleMapsConfigured,
  loadGoogleMaps,
  roundCoordinate,
} from '../../lib/googleMaps'
import { GEOCODE_DEBOUNCE_MS, MAX_PLACE_SUGGESTIONS } from '../../lib/constants'
import { Input } from '../ui/input'
import { Label } from '../ui/label'

/** The three fields the picker owns on the node form. */
export interface PickedLocation {
  latitude: string
  longitude: string
  addressLine: string
}

export interface LocationPickerProps {
  value: PickedLocation
  onChange: (next: PickedLocation) => void
  /** Rendered under the map; use it for the API's field-level error. */
  error?: string
}

/** One row in the search dropdown, normalised across Places and Geocoder results. */
interface PlaceSuggestion {
  id: string
  primary: string
  secondary: string
  /** Present for Geocoder hits, which already carry coordinates. */
  position?: google.maps.LatLngLiteral
  /** Present for Places hits, which need a follow-up fetch. */
  placePrediction?: google.maps.places.PlacePrediction
}

type MapStatus = 'idle' | 'loading' | 'ready' | 'error'

export default function LocationPicker({ value, onChange, error }: LocationPickerProps) {
  const mapNodeRef = useRef<HTMLDivElement | null>(null)
  const mapRef = useRef<google.maps.Map | null>(null)
  const markerRef = useRef<google.maps.marker.AdvancedMarkerElement | null>(null)
  const geocoderRef = useRef<google.maps.Geocoder | null>(null)
  const sessionTokenRef = useRef<google.maps.places.AutocompleteSessionToken | null>(null)
  // Guards against a slow reverse-geocode overwriting a newer pin's address.
  const geocodeSeqRef = useRef(0)
  // Choosing a suggestion rewrites the query box; that must not re-open the list.
  const suppressSearchRef = useRef(false)

  const [status, setStatus] = useState<MapStatus>('idle')
  const [statusMessage, setStatusMessage] = useState('')
  const [query, setQuery] = useState('')
  const [suggestions, setSuggestions] = useState<PlaceSuggestion[]>([])
  const [isSearching, setSearching] = useState(false)
  const [isLocating, setLocating] = useState(false)

  const latitude = Number(value.latitude)
  const longitude = Number(value.longitude)
  const hasPin =
    value.latitude !== '' && value.longitude !== '' && !Number.isNaN(latitude) && !Number.isNaN(longitude)

  // `onChange` is read through a ref inside map callbacks so the map is built
  // once and never torn down just because the parent re-rendered.
  const onChangeRef = useRef(onChange)
  onChangeRef.current = onChange
  const valueRef = useRef(value)
  valueRef.current = value

  /** Writes a picked position back to the form, optionally with a fresh address. */
  const commitPosition = useCallback((position: google.maps.LatLngLiteral, addressLine?: string) => {
    onChangeRef.current({
      latitude: String(roundCoordinate(position.lat)),
      longitude: String(roundCoordinate(position.lng)),
      addressLine: addressLine ?? valueRef.current.addressLine,
    })
  }, [])

  /** Looks up a human address for a dropped pin so the operator rarely types one. */
  const reverseGeocode = useCallback(
    async (position: google.maps.LatLngLiteral) => {
      const geocoder = geocoderRef.current
      if (!geocoder) return

      const seq = ++geocodeSeqRef.current
      try {
        const { results } = await geocoder.geocode({ location: position })
        // A newer pin landed while this was in flight - discard the stale answer.
        if (seq !== geocodeSeqRef.current) return
        const formatted = results[0]?.formatted_address
        if (formatted) commitPosition(position, formatted)
      } catch {
        // A failed lookup is not an error the operator needs to act on - the
        // coordinates are already correct and the address stays editable.
      }
    },
    [commitPosition],
  )

  /** Moves the pin, recentres the map and refreshes the address line. */
  const placePin = useCallback(
    (position: google.maps.LatLngLiteral, addressLine?: string) => {
      commitPosition(position, addressLine)

      const map = mapRef.current
      const marker = markerRef.current
      if (map && marker) {
        marker.position = position
        map.panTo(position)
        if ((map.getZoom() ?? DEFAULT_MAP_ZOOM) < PINNED_MAP_ZOOM) map.setZoom(PINNED_MAP_ZOOM)
      }

      if (!addressLine) void reverseGeocode(position)
    },
    [commitPosition, reverseGeocode],
  )

  // Build the map once, on mount. Everything after this point mutates it.
  useEffect(() => {
    if (!isGoogleMapsConfigured()) {
      setStatus('error')
      setStatusMessage('No Google Maps key is configured, so the map is unavailable.')
      return
    }

    let cancelled = false
    setStatus('loading')

    loadGoogleMaps()
      .then(async (maps) => {
        if (cancelled || !mapNodeRef.current) return

        const { Map } = (await maps.importLibrary('maps')) as google.maps.MapsLibrary
        const { AdvancedMarkerElement } = (await maps.importLibrary(
          'marker',
        )) as google.maps.MarkerLibrary
        if (cancelled || !mapNodeRef.current) return

        const start = valueRef.current
        const startLat = Number(start.latitude)
        const startLng = Number(start.longitude)
        const hasStart =
          start.latitude !== '' && start.longitude !== '' && !Number.isNaN(startLat) && !Number.isNaN(startLng)
        const center = hasStart ? { lat: startLat, lng: startLng } : DEFAULT_MAP_CENTER

        const map = new Map(mapNodeRef.current, {
          center,
          zoom: hasStart ? PINNED_MAP_ZOOM : DEFAULT_MAP_ZOOM,
          mapId: SUNGRID_MAP_ID,
          disableDefaultUI: true,
          zoomControl: true,
          clickableIcons: false,
          gestureHandling: 'greedy',
        })

        const marker = new AdvancedMarkerElement({
          map,
          position: hasStart ? center : null,
          gmpDraggable: true,
          title: 'Node location',
        })

        // Click anywhere to drop the pin; drag it for fine adjustment.
        map.addListener('click', (event: google.maps.MapMouseEvent) => {
          if (event.latLng) placePin(event.latLng.toJSON())
        })
        marker.addListener('dragend', () => {
          const position = marker.position
          if (position) placePin({ lat: Number(position.lat), lng: Number(position.lng) })
        })

        mapRef.current = map
        markerRef.current = marker
        geocoderRef.current = new maps.Geocoder()
        setStatus('ready')
      })
      .catch(() => {
        if (cancelled) return
        setStatus('error')
        setStatusMessage('The map could not be loaded. Try again in a moment.')
      })

    return () => {
      cancelled = true
    }
  }, [placePin])

  // Keep the marker in step with coordinates typed into the manual fields.
  useEffect(() => {
    const marker = markerRef.current
    if (!marker || status !== 'ready') return
    marker.position = hasPin ? { lat: latitude, lng: longitude } : null
  }, [hasPin, latitude, longitude, status])

  // Debounced place search. Places (New) gives the better suggestions; the
  // Geocoder is the fallback when only the Geocoding API is enabled on the key.
  useEffect(() => {
    // The query box was filled in by picking a suggestion, not by typing.
    if (suppressSearchRef.current) {
      suppressSearchRef.current = false
      setSuggestions([])
      setSearching(false)
      return
    }

    const trimmed = query.trim()
    if (status !== 'ready' || trimmed.length < 3) {
      setSuggestions([])
      setSearching(false)
      return
    }

    let cancelled = false
    setSearching(true)

    const timer = window.setTimeout(async () => {
      try {
        const maps = window.google.maps
        const places = (await maps.importLibrary('places')) as google.maps.PlacesLibrary
        sessionTokenRef.current ??= new places.AutocompleteSessionToken()

        const { suggestions: found } = await places.AutocompleteSuggestion.fetchAutocompleteSuggestions({
          input: trimmed,
          sessionToken: sessionTokenRef.current,
        })
        if (cancelled) return

        setSuggestions(
          found
            .flatMap((suggestion) => (suggestion.placePrediction ? [suggestion.placePrediction] : []))
            .slice(0, MAX_PLACE_SUGGESTIONS)
            .map((prediction) => ({
              id: prediction.placeId,
              primary: prediction.mainText?.toString() ?? prediction.text.toString(),
              secondary: prediction.secondaryText?.toString() ?? '',
              placePrediction: prediction,
            })),
        )
      } catch {
        // Places unavailable - fall back to geocoding the raw query string.
        try {
          const geocoder = geocoderRef.current
          if (!geocoder) throw new Error('No geocoder')
          const { results } = await geocoder.geocode({ address: trimmed })
          if (cancelled) return
          setSuggestions(
            results.slice(0, MAX_PLACE_SUGGESTIONS).map((result) => ({
              id: result.place_id,
              primary: result.formatted_address,
              secondary: '',
              position: result.geometry.location.toJSON(),
            })),
          )
        } catch {
          if (!cancelled) setSuggestions([])
        }
      } finally {
        if (!cancelled) setSearching(false)
      }
    }, GEOCODE_DEBOUNCE_MS)

    return () => {
      cancelled = true
      window.clearTimeout(timer)
    }
  }, [query, status])

  /** Applies a chosen suggestion: pin it, label it, and close the dropdown. */
  const handleSelectSuggestion = async (suggestion: PlaceSuggestion) => {
    setSuggestions([])

    if (suggestion.position) {
      suppressSearchRef.current = true
      setQuery(suggestion.primary)
      placePin(suggestion.position, suggestion.primary)
      return
    }

    const prediction = suggestion.placePrediction
    if (!prediction) return

    try {
      const place = prediction.toPlace()
      await place.fetchFields({ fields: ['location', 'formattedAddress', 'displayName'] })
      // The token is single-use per resolved place; drop it so the next search bills fresh.
      sessionTokenRef.current = null

      const location = place.location
      if (!location) return
      const addressLine = place.formattedAddress ?? suggestion.primary
      suppressSearchRef.current = true
      setQuery(addressLine)
      placePin({ lat: location.lat(), lng: location.lng() }, addressLine)
    } catch {
      setStatusMessage('That place could not be resolved. Try another search or drop a pin.')
    }
  }

  /** Centres on the operator's own device position - handy when they are on site. */
  const handleUseMyLocation = () => {
    if (!navigator.geolocation) {
      setStatusMessage('This browser cannot share a location.')
      return
    }

    setLocating(true)
    navigator.geolocation.getCurrentPosition(
      (position) => {
        setLocating(false)
        placePin({ lat: position.coords.latitude, lng: position.coords.longitude })
      },
      () => {
        setLocating(false)
        setStatusMessage('Location permission was denied. Search or drop a pin instead.')
      },
      { enableHighAccuracy: true, timeout: 10000 },
    )
  }

  const coordinateLabel = useMemo(
    () => (hasPin ? formatCoordinates(latitude, longitude) : 'No location selected'),
    [hasPin, latitude, longitude],
  )

  return (
    <div className="flex flex-col gap-3">
      <Label htmlFor="node-location-search">Location</Label>

      {/* Search box + suggestion list */}
      <div className="sg-map-search">
        <Search size={15} className="sg-map-search-icon" aria-hidden="true" />
        <Input
          id="node-location-search"
          className="pl-9 pr-9"
          placeholder={status === 'ready' ? 'Search a place or address...' : 'Map unavailable'}
          value={query}
          disabled={status !== 'ready'}
          autoComplete="off"
          onChange={(event) => setQuery(event.target.value)}
          onKeyDown={(event) => {
            // The picker sits inside the node form; Enter must not submit it.
            if (event.key === 'Enter') event.preventDefault()
            if (event.key === 'Escape') setSuggestions([])
          }}
        />
        {isSearching ? (
          <Loader2 size={15} className="sg-map-search-trailing animate-spin" aria-hidden="true" />
        ) : query ? (
          <button
            type="button"
            className="sg-map-search-trailing sg-map-search-clear"
            onClick={() => {
              setQuery('')
              setSuggestions([])
            }}
            aria-label="Clear search"
          >
            <X size={15} />
          </button>
        ) : null}

        {suggestions.length > 0 ? (
          <ul className="sg-map-suggestions" role="listbox" aria-label="Place suggestions">
            {suggestions.map((suggestion) => (
              <li key={suggestion.id}>
                <button
                  type="button"
                  className="sg-map-suggestion"
                  role="option"
                  aria-selected="false"
                  onClick={() => void handleSelectSuggestion(suggestion)}
                >
                  <MapPin size={14} aria-hidden="true" />
                  <span className="sg-map-suggestion-text">
                    <span className="sg-map-suggestion-primary">{suggestion.primary}</span>
                    {suggestion.secondary ? (
                      <span className="sg-map-suggestion-secondary">{suggestion.secondary}</span>
                    ) : null}
                  </span>
                </button>
              </li>
            ))}
          </ul>
        ) : null}
      </div>

      {/* The map itself, with its loading and error states */}
      <div className="sg-map-frame">
        <div ref={mapNodeRef} className="sg-map-canvas" role="application" aria-label="Node location map" />

        {status === 'loading' ? (
          <div className="sg-map-overlay">
            <Loader2 size={18} className="animate-spin" aria-hidden="true" />
            <p className="sg-body-sm">Loading map...</p>
          </div>
        ) : null}

        {status === 'error' ? (
          <div className="sg-map-overlay">
            <MapPin size={20} aria-hidden="true" />
            <p className="sg-body-sm">Map unavailable</p>
            <p className="sg-caption">Reopen the panel to try again.</p>
          </div>
        ) : null}

        {status === 'ready' ? (
          <button
            type="button"
            className="sg-map-locate"
            onClick={handleUseMyLocation}
            disabled={isLocating}
            aria-label="Use my current location"
          >
            {isLocating ? (
              <Loader2 size={15} className="animate-spin" aria-hidden="true" />
            ) : (
              <Crosshair size={15} aria-hidden="true" />
            )}
          </button>
        ) : null}
      </div>

      {/* What was picked, in plain text - never colour alone (design system 23) */}
      <div className="sg-map-readout">
        <MapPin size={14} className={hasPin ? 'text-sg-primary' : 'text-sg-text-muted'} aria-hidden="true" />
        <span className={hasPin ? 'font-mono' : 'text-sg-text-muted'}>{coordinateLabel}</span>
        {status === 'ready' ? (
          <span className="sg-map-readout-hint">Click the map or drag the pin to adjust</span>
        ) : null}
      </div>

      {statusMessage ? <p className="sg-helper">{statusMessage}</p> : null}

      <div className="flex flex-col gap-2">
        <Label htmlFor="addressLine">Address</Label>
        <Input
          id="addressLine"
          value={value.addressLine}
          placeholder="Filled in from the map - edit if needed"
          onChange={(event) => onChange({ ...value, addressLine: event.target.value })}
          required
        />
      </div>

      {error ? (
        <p className="sg-error-text" role="alert">
          {error}
        </p>
      ) : null}
    </div>
  )
}
