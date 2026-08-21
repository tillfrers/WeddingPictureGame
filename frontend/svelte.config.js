import adapter from '@sveltejs/adapter-static';
import { vitePreprocess } from '@sveltejs/vite-plugin-svelte';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	preprocess: vitePreprocess(),

	kit: {
		// Reines SPA: der Tischcode ist ein dynamischer Routen-Parameter, der
		// nicht vorab gerendert werden kann. `fallback` liefert für jeden Pfad
		// dieselbe index.html aus, das Routing passiert danach im Browser.
		adapter: adapter({
			pages: 'build',
			assets: 'build',
			fallback: 'index.html',
			precompress: false,
			strict: false
		})
	}
};

export default config;
