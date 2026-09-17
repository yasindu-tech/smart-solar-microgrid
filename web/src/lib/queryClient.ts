import { QueryClient } from '@tanstack/react-query'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      // A 401/403 will not fix itself on retry; only network hiccups should.
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
})
