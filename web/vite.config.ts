import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Vite configuration. The API base URL comes from VITE_API_BASE_URL so the
// same build can point at localhost or the hosted IIS instance.
export default defineConfig({
  plugins: [react()],
  server: { port: 5173 },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
      '@components': fileURLToPath(new URL('./src/components', import.meta.url)),
      '@pages': fileURLToPath(new URL('./src/pages', import.meta.url)),
      '@api': fileURLToPath(new URL('./src/api', import.meta.url)),
    },
  },
})
