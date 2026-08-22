import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig, loadEnv } from 'vite';

// Das Backend setzt keine CORS-Header (und darf nicht angepasst werden).
// Deshalb proxyt Vite /api-Aufrufe serverseitig zum Backend, sodass der
// Browser alles same-origin sieht - sowohl im Dev-Server als auch im
// `vite preview`. Ziel per VITE_API_TARGET (.env) überschreibbar.
//
// Wichtig: `.env`-Dateien werden von Vite NICHT automatisch in
// `process.env` geladen (das gilt nur für Client-Code via
// `import.meta.env`). Für die Config-Datei selbst muss `loadEnv`
// explizit aufgerufen werden, sonst wird die `.env` hier ignoriert.
export default defineConfig(({ mode }) => {
	const env = loadEnv(mode, process.cwd(), '');
	const apiTarget = env.VITE_API_TARGET ?? 'http://localhost:5037';

	return {
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
	};
});
