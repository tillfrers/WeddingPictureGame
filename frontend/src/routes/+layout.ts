import { redirect } from '@sveltejs/kit';
import { imageClient } from '$lib/api';
import { RedeemDto } from '$lib/api/client';
import type { LayoutLoad } from './$types';

// Reine SPA gegen ein REST-Backend: kein SSR nötig, Adapter liefert dafür
// überall dieselbe index.html aus (siehe svelte.config.js -> fallback).
export const ssr = false;

/** Pfad der Galerie über alle Tische - die einzige Route hinter dem Einladungs-Token. */
const GESAMTGALERIE = '/gallery';

/**
 * Steht der weitergehende Zugang schon? Das Backend führt nur einen einzigen
 * Auth-Cookie, und der ist HttpOnly - auslesen geht also nicht, nachfragen
 * schon.
 */
async function hatGesamtzugang(): Promise<boolean> {
	try {
		await imageClient.getGalleryAll(1);
		return true;
	} catch {
		// 401 (gar kein Zugang) wie 403 (nur Tisch-Zugang) landen hier, ebenso
		// ein Netzwerkfehler. In allen drei Fällen ist Einlösen richtig.
		return false;
	}
}

/**
 * Alle Backend-Endpunkte (ausser /redeem) verlangen den Auth-Cookie. Den gibt
 * es nur gegen ein Capability-Token, und davon gibt es zwei:
 *
 *   /4D60?token=...     -> Tisch-Token, öffnet die Tisch-Endpunkte (QR-Code am Tisch)
 *   /gallery?token=...  -> Einladungs-Token, öffnet zusätzlich die Galerie
 *                          über alle Tische (Einladungslink nach der Hochzeit)
 *
 * Welches der beiden gemeint ist, verrät das Ziel des Links - deshalb hängt
 * `allTables` am Pfad. Das Backend prüft je nach Flag gegen einen anderen Hash
 * und hinterlegt das Ergebnis als Scope im Cookie; ein Tisch-Token kommt damit
 * nicht an die Gesamtgalerie.
 *
 * Weil es aber nur EINEN Cookie gibt, überschreibt jedes Einlösen das vorige.
 * Wer den Einladungslink schon offen hatte und danach den QR-Code am Tisch
 * scannt, verlöre sonst die Gesamtgalerie - deshalb wird das Tisch-Token
 * übersprungen, wenn der weitergehende Zugang bereits steht. Er deckt die
 * Tisch-Endpunkte ohnehin mit ab.
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

	// Ein von Hand angehängter Schrägstrich soll den Link nicht kaputt machen.
	const allTables = url.pathname.replace(/\/+$/, '') === GESAMTGALERIE;

	if (allTables || !(await hatGesamtzugang())) {
		try {
			await imageClient.redeem(new RedeemDto({ token, allTables }));
		} catch {
			// Ungültiges Token: nicht hier abbrechen, sondern den Redirect trotzdem
			// ausführen. Die Seite läuft dann in ihren 401 und zeigt den passenden
			// Hinweis - so bleibt die Fehlermeldung an einer Stelle.
		}
	}

	const target = new URL(url);
	target.searchParams.delete('token');

	redirect(307, `${target.pathname}${target.search}`);
};
