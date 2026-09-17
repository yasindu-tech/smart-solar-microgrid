import { AxiosError } from 'axios'
import type { ApiError } from '../types/CommonTypes'

// User-facing copy for the error codes a caller can actually hit here.
// Codes not listed fall back to the server's own message.
const MESSAGES_BY_CODE: Record<string, string> = {
  E4301: 'Incorrect email/NIC or password.',
  E4302: 'This account is not active yet. Contact Backoffice for help.',
  E4303: 'You do not have permission to do that.',
  E5000: 'Something went wrong on our end. Please try again.',
}

function envelopeOf(error: AxiosError): ApiError | undefined {
  return error.response?.data as ApiError | undefined
}

// Resolve any thrown value to a single display-ready string. Never throws.
export function getErrorMessage(error: unknown): string {
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
  return error instanceof AxiosError && envelopeOf(error)?.code === 'E4301'
}
