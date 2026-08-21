import { ImageClient } from './client';

/**
 * Das Backend liefert Fehler oft als reinen JSON-String zurück
 * (`BadRequest("Dieser Tisch existiert nicht")`), nicht als ProblemDetails-
 * Objekt. `ProblemDetails.fromJS` im generierten Client verwirft nicht-
 * objektartige Bodies stillschweigend (`data = typeof data === 'object' ?
 * data : {}`), wodurch die eigentliche Meldung verloren geht. Wir wandeln
 * einen bloßen String-Body deshalb hier in ein kompatibles Objekt um, bevor
 * der generierte Client ihn parst.
 */
const apiFetch: typeof fetch = async (input, init) => {
	const response = await fetch(input, init);
	if (response.ok) return response;

	const contentType = response.headers.get('content-type') ?? '';
	if (!contentType.includes('json')) return response;

	const rawText = await response.clone().text();
	let parsed: unknown;
	try {
		parsed = JSON.parse(rawText);
	} catch {
		return response;
	}

	if (typeof parsed !== 'string') return response;

	const body = JSON.stringify({ title: parsed, detail: parsed });
	return new Response(body, {
		status: response.status,
		statusText: response.statusText,
		headers: response.headers
	});
};

// Leerer String => relative Aufrufe ("/api/..."), die im Dev-Server sowie im
// `vite preview` per Proxy zum Backend weitergeleitet werden (siehe
// vite.config.ts). So sieht der Browser nur same-origin-Requests, obwohl das
// Backend selbst kein CORS erlaubt.
export const imageClient = new ImageClient('', { fetch: apiFetch });

export function displayUrl(tisch: string, id: string): string {
	return `/api/Image/${encodeURIComponent(tisch)}/${id}/display`;
}

/** Extrahiert eine für Menschen lesbare Fehlermeldung aus einer fehlgeschlagenen API-Antwort. */
export function apiErrorMessage(err: unknown): string {
	if (err && typeof err === 'object') {
		const e = err as Record<string, unknown>;

		if (typeof e.detail === 'string' && e.detail) return e.detail;
		if (typeof e.title === 'string' && e.title) return e.title;

		if (typeof e.response === 'string' && e.response) {
			try {
				const parsed = JSON.parse(e.response);
				if (typeof parsed === 'string' && parsed) return parsed;
			} catch {
				// response war kein JSON-String, unten weiter mit Rohtext
			}
			return e.response;
		}
	}

	return 'Unbekannter Fehler. Bitte versuche es erneut.';
}
