/** The three location fields the picker owns on the node form. */
export interface PickedLocation {
  latitude: string
  longitude: string
  addressLine: string
}

/** One row in the search dropdown, normalised across Places and Geocoder results. */
export interface PlaceSuggestion {
  id: string
  primary: string
  secondary: string
  /** Present for Geocoder hits, which already carry coordinates. */
  position?: google.maps.LatLngLiteral
  /** Present for Places hits, which need a follow-up fetch. */
  placePrediction?: google.maps.places.PlacePrediction
}

export type MapStatus = 'idle' | 'loading' | 'ready' | 'error'
