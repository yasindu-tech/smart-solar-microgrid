/// <reference types="vite/client" />
/// <reference types="google.maps" />

/**
 * Typed environment variables. Add every VITE_* variable here so a typo in a
 * variable name is a compile error rather than an undefined at runtime.
 */
interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string
  /**
   * Browser key for the Google Maps JavaScript API. May be absent, in which
   * case the node location picker falls back to manual coordinate entry.
   */
  readonly VITE_GOOGLE_MAP_API_KEY?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
