import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Em desenvolvimento o Vite repassa /api e /health para a API .NET,
// então o navegador fala com uma única origem e não é preciso configurar CORS.
const apiUrl = 'http://localhost:5033'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': apiUrl,
      '/health': apiUrl,
    },
  },
})
