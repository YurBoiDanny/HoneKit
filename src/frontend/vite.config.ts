import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// In the published stack nginx does this proxying (see nginx.conf).
// This block is only for `npm run dev` against services started by the AppHost,
// which injects services__<resource>__http__0 for every referenced service.
const service = (name: string, fallback: string) =>
  (process.env[`services__${name}__http__0`] ?? fallback).replace(/\/$/, '');

const apiProxy = (prefix: string, name: string, fallback: string) => ({
  target: service(name, fallback),
  changeOrigin: true,
  rewrite: (path: string) => path.replace(new RegExp(`^/api/${prefix}`), '/api'),
});

const apiPass = (name: string, fallback: string) => ({
  target: service(name, fallback),
  changeOrigin: true,
});

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api/content': apiProxy('content', 'content', 'http://localhost:5301'),
      '/api/progress': apiProxy('progress', 'progress', 'http://localhost:5302'),
      '/api/roster': apiProxy('roster', 'roster', 'http://localhost:5303'),
      '/api/requirements': { ...apiPass('requirements', 'http://localhost:5304'), timeout: 100_000, proxyTimeout: 100_000 },
      '/api/recommendations': apiPass('recommendations', 'http://localhost:5305'),
      '/api/plans': apiPass('recommendations', 'http://localhost:5305'),
    },
  },
});
