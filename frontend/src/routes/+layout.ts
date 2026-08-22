import { redirect } from '@sveltejs/kit';
import { imageClient } from '$lib/api';
import { RedeemDto } from '$lib/api/client';
import type { LayoutLoad } from './$types';

// Reine SPA gegen ein REST-Backend: kein SSR nötig, Adapter liefert dafür
// überall dieselbe index.html aus (siehe svelte.config.js -> fallback).
export const ssr = false;

/**
 * Alle Backend-Endpunkte (ausser /redeem) verlangen den Auth-Cookie. Den gibt
 * es nur gegen das Capability-Token aus dem QR-Code bzw. dem Einladungslink:
 *
 *   /9E79?token=...      -> Tisch-Galerie   (QR-Code am Tisch)
 *   /gallery?token=...   -> alle Tische     (Einladungslink nach der Hochzeit)
 *
 * Das Token wird hier einmal eingeloest und danach per Redirect aus der URL
 * entfernt, damit es nicht in History, Lesezeichen oder geteilten Links landet.
 * Weil Layout- und Page-Loads parallel laufen, muessen die Galerie-Loads
 * `await parent()` aufrufen - sonst fragen sie das Backend an, bevor der
 * Cookie gesetzt ist.
 */
export const load: LayoutLoad = async ({ url }) => {
	const token = url.searchParams.get('token');

	if (!token) return;

	try {
		await imageClient.redeem(new RedeemDto({ token }));
	} catch {
		// Ungültiges Token: nicht hier abbrechen, sondern den Redirect trotzdem
		// ausführen. Die Seite läuft dann in ihren 401 und zeigt den passenden
		// Hinweis - so bleibt die Fehlermeldung an einer Stelle.
	}

	const target = new URL(url);
	target.searchParams.delete('token');

	redirect(307, `${target.pathname}${target.search}`);
};
