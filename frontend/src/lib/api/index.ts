import { ImageClient } from './client';

// Leerer String => relative Aufrufe ("/api/..."), die im Dev-Server sowie im
// `vite preview` per Proxy zum Backend weitergeleitet werden (siehe
// vite.config.ts). So sieht der Browser nur same-origin-Requests, obwohl das
// Backend selbst kein CORS erlaubt.
export const imageClient = new ImageClient('');

export function displayUrl(tisch: string, id: string): string {
	return `/api/Image/${encodeURIComponent(tisch)}/${id}/display`;
}

/** Extrahiert eine für Menschen lesbare Fehlermeldung aus einer ProblemDetails-Antwort. */
export function apiErrorMessage(err: unknown): string {
	if (err && typeof err === 'object') {
		const e = err as Record<string, unknown>;

		if (typeof e.detail === 'string' && e.detail) return e.detail;
		if (typeof e.title === 'string' && e.title) return e.title;
	}

	return 'Unbekannter Fehler. Bitte versuche es erneut.';
}
