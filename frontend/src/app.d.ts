// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces
declare global {
	namespace App {
		// interface Error {}
		// interface Locals {}
		// interface PageData {}
		interface PageState {
			/** Index des im Viewer geöffneten Bildes (Shallow Routing). */
			viewer?: number;
		}
		// interface Platform {}
	}
}

export {};
