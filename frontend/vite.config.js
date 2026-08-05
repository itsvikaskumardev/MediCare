import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'
import { fileURLToPath } from 'url'

const __dirname = path.dirname(fileURLToPath(import.meta.url))

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const hasValidClerkKey =
    !!env.VITE_CLERK_PUBLISHABLE_KEY &&
    !env.VITE_CLERK_PUBLISHABLE_KEY.includes('c3RhdGljLWRlbW8')

  return {
    plugins: [react(), tailwindcss()],
    envPrefix: ['VITE_', 'BACKEND_URL'],
    resolve: {
      alias: hasValidClerkKey
        ? []
        : [
            {
              find: '@clerk/clerk-react',
              replacement: path.resolve(__dirname, 'src/clerkFallback.jsx'),
            },
          ],
    },
  }
})
