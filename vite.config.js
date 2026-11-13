import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  // GitHub Pages serves this repo at https://edward-hsu-1994.github.io/OpenAlprWrapper/
  base: '/OpenAlprWrapper/',
  plugins: [react()],
  build: { outDir: 'dist' },
});
