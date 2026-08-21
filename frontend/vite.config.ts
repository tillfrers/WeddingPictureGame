import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

// Das Backend setzt keine CORS-Header (und darf nicht angepasst werden).
// Deshalb proxyt Vite /api-Aufrufe serverseitig zum Backend, sodass der
// Browser alles same-origin sieht - sowohl im Dev-Server als auch im
// `vite preview`. Ziel per VITE_API_TARGET überschreibbar.
const apiTarget = process.env.VITE_API_TARGET ?? 'http://localhost:5037';

export default defineConfig({
	plugins: [sveltekit()],
	server: {
		proxy: {
			'/api': {
				target: apiTarget,
				changeOrigin: true
			}
		}
	},
	preview: {
		proxy: {
			'/api': {
				target: apiTarget,
				changeOrigin: true
			}
		}
	}
});
