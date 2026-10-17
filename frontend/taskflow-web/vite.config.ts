/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Em desenvolvimento o Vite repassa /api, /hubs (SignalR, com WebSocket) e /health para a API .NET,
// então o navegador fala com uma única origem e não é preciso configurar CORS.
const apiUrl = 'http://localhost:5033'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': apiUrl,
      '/health': apiUrl,
      '/hubs': { target: apiUrl, ws: true },
    },
  },
  // Sem jsdom/testing-library por enquanto: só testamos utils puros (sem DOM).
  test: {
    environment: 'node',
  },
})
