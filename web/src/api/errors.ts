import { AxiosError } from 'axios'
import type { ApiError } from '../types/CommonTypes'
import { ApiRequestError } from './types'

// User-facing copy for the error codes a caller can actually hit here.
// Codes not listed fall back to the server's own message. Login has its own
// handler (hooks.ts), so here E4301 only ever means an invalid or expired token.
const MESSAGES_BY_CODE: Record<string, string> = {
  E4301: 'Your session has expired. Please sign in again.',
  E4302: 'This account is not active yet. Contact Backoffice for help.',
  E4303: 'You do not have permission to do that.',
  E4404: 'The requested record could not be found.',
  E5000: 'Something went wrong on our end. Please try again.',
}

function envelopeOf(error: AxiosError): ApiError | undefined {
  return error.response?.data as ApiError | undefined
}

// Resolve any thrown value to a single display-ready string. Never throws.
export function getErrorMessage(error: unknown): string {
  // client.ts rejects every failed request with ApiRequestError, not the raw AxiosError.
  if (error instanceof ApiRequestError) {
    return MESSAGES_BY_CODE[error.envelope.code] ?? error.envelope.message
  }

  if (error instanceof AxiosError) {
    const envelope = envelopeOf(error)
    if (envelope) {
      return MESSAGES_BY_CODE[envelope.code] ?? envelope.message
    }
    return error.message
  }

  if (error instanceof Error) {
    return error.message
  }

  return 'Something went wrong. Please try again.'
}

// True when the error means "the session is gone" - callers use this to
// redirect to login instead of showing an inline message.
export function isUnauthorized(error: unknown): boolean {
  if (error instanceof ApiRequestError) {
    return error.envelope.code === 'E4301'
  }
  return error instanceof AxiosError && envelopeOf(error)?.code === 'E4301'
}
