import { ImageClient } from './client';

// Leerer String => relative Aufrufe ("/api/..."), die im Dev-Server sowie im
// `vite preview` per Proxy zum Backend weitergeleitet werden (siehe
// vite.config.ts). So sieht der Browser nur same-origin-Requests, obwohl das
// Backend selbst kein CORS erlaubt. Same-origin heisst ausserdem, dass der
// Auth-Cookie aus /redeem automatisch mitgeschickt wird.
export const imageClient = new ImageClient('');

/**
 * Das Backend liefert pro Bild nur die Thumbnail-URL. In der Galerie ueber
 * alle Tische stammen die Bilder aus verschiedenen Tischen, der Tischcode
 * steckt also nur in dieser URL - deshalb wird die Vollbild-URL daraus
 * abgeleitet statt aus dem Tisch der Seite.
 */
export function displayUrl(thumbnailUrl: string): string {
	return thumbnailUrl.replace(/\/thumbnail$/, '/display');
}

/** HTTP-Status einer fehlgeschlagenen API-Antwort, 400 als Fallback. */
export function apiErrorStatus(err: unknown): number {
	if (err && typeof err === 'object') {
		const status = (err as Record<string, unknown>).status;

		if (typeof status === 'number' && status >= 400 && status < 600) return status;
	}

	return 400;
}

/** Extrahiert eine für Menschen lesbare Fehlermeldung aus einer ProblemDetails-Antwort. */
export function apiErrorMessage(err: unknown): string {
	// fetch wirft bei Netzwerkfehlern und Abbrüchen einen TypeError bzw. einen
	// AbortError - ganz ohne Status und ohne Body. Auf dem Handy passiert das,
	// wenn der Browser die Seite mitten in einer Anfrage anhält; im Backend-Log
	// steht davon nichts, weil die Anfrage dort nie ankommt.
	if (err instanceof TypeError || (err instanceof Error && err.name === 'AbortError'))
		return 'Die Verbindung wurde unterbrochen. Bitte versuche es noch einmal.';

	// 401 und 403 kommen aus der Autorisierung ohne verwertbaren Body - da gibt
	// es nichts zu extrahieren, der Gast braucht aber trotzdem einen Hinweis.
	const status = apiErrorStatus(err);

	if (status === 401)
		return 'Dieser Zugang ist nicht (mehr) gültig. Bitte scanne den QR-Code an deinem Tisch erneut.';

	// Eingelöst, aber mit dem falschen Token: das Tisch-Token aus dem QR-Code
	// öffnet nur den eigenen Tisch, nicht die Galerie über alle Tische.
	if (status === 403)
		return 'Dieser Zugang zeigt nur die Fotos deines Tisches. Für alle Fotos brauchst du den Einladungslink.';

	if (err && typeof err === 'object') {
		const e = err as Record<string, unknown>;

		if (typeof e.detail === 'string' && e.detail) return e.detail;
		if (typeof e.title === 'string' && e.title) return e.title;
	}

	return 'Unbekannter Fehler. Bitte versuche es erneut.';
}
