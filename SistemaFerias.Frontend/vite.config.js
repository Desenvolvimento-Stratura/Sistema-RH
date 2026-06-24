import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    host: '10.123.97.137',
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:5190',
        changeOrigin: true,
      }
    }
  }
})