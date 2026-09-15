/// <reference types="vite/client" />

/**
 * Typed environment variables. Add every VITE_* variable here so a typo in a
 * variable name is a compile error rather than an undefined at runtime.
 */
interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
