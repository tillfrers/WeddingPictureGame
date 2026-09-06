/**
 * Drei Wege, ein Bild aufs Gerät zu bekommen - `sicherWeg()` entscheidet.
 *
 * Auf dem Handy ist das Teilen-Menü der einzige Weg, der die Fotos wirklich in
 * der Galerie ablegt: iOS legt Downloads in "Dateien" statt in "Fotos", und
 * eine Folge programmgesteuerter Downloads bricht dort ohnehin nach dem ersten
 * Bild ab. Über `navigator.share` reicht dagegen ein Tipp auf "Bilder sichern".
 *
 * Firefox unterstützt das Teilen von Dateien nicht (weder Desktop noch
 * Android) und muss deshalb herunterladen. Auf dem Handy zeigt es dabei einen
 * modalen Speichern-Dialog und verträgt immer nur einen davon: Klicks, die
 * währenddessen kommen, bleiben in der Warteschlange hängen und tauchen erst
 * wieder auf, wenn die App aus dem Hintergrund zurückkehrt. Deshalb wird dort
 * pro Tipp genau ein Bild gespeichert.
 *
 * Am Rechner ist beides kein Thema - dort laufen die Downloads einfach durch.
 */
export type SicherWeg =
	/** Stapelweise ans Teilen-Menü des Geräts. */
	| 'teilen'
	/** Ein Bild pro Tipp herunterladen (mobile Browser ohne Datei-Teilen). */
	| 'einzeln'
	/** Alle Bilder nacheinander herunterladen, ohne Zutun. */
	| 'auto';

/**
 * Chromium nimmt höchstens 10 Dateien und 50 MB pro Teilen-Aufruf entgegen;
 * iOS kann mehr, aber ein gemeinsamer Stapel spart eine Sonderbehandlung.
 * Knapp unter dem Limit, damit Metadaten nicht drüberkippen.
 */
export const STAPEL_MAX_DATEIEN = 10;
export const STAPEL_MAX_BYTES = 45 * 1024 * 1024;

/**
 * Firefox kennt `canShare`, unterstützt aber keine Dateien - das merkt man erst,
 * wenn man mit einer echten Datei nachfragt. Deshalb die Probe statt einer
 * reinen Existenzprüfung.
 */
function kannDateienTeilen(): boolean {
	if (typeof navigator === 'undefined') return false;
	if (typeof navigator.share !== 'function' || typeof navigator.canShare !== 'function')
		return false;

	try {
		const probe = new File(['x'], 'probe.jpg', { type: 'image/jpeg' });
		return navigator.canShare({ files: [probe] });
	} catch {
		return false;
	}
}

export function sicherWeg(): SicherWeg {
	// Kein Touch-Gerät: am Rechner will man die Bilder im Download-Ordner, nicht
	// im Teilen-Dialog des Betriebssystems - und dort laufen Downloads in Folge.
	if (typeof window === 'undefined' || !window.matchMedia?.('(pointer: coarse)').matches)
		return 'auto';

	return kannDateienTeilen() ? 'teilen' : 'einzeln';
}

/**
 * Öffnet das Teilen-Menü für einen Stapel Bilder.
 *
 * Muss aus einem frischen Tipp heraus aufgerufen werden - nach einem `await`
 * ist die Nutzeraktion verbraucht und iOS lehnt ab. Deshalb wird der Stapel
 * vorher geladen und erst auf Knopfdruck geteilt.
 *
 * @returns true, wenn geteilt wurde; false, wenn der Gast abgebrochen hat.
 */
export async function teileDateien(files: File[]): Promise<boolean> {
	try {
		await navigator.share({ files });
		return true;
	} catch (err) {
		// Abbruch ist kein Fehler - der Gast hat das Menü einfach zugemacht.
		if (err instanceof Error && err.name === 'AbortError') return false;
		throw err;
	}
}

/**
 * Solange eine Objekt-URL offen ist, hält sie das ganze Bild im Speicher. Ein
 * Original wiegt schnell mehrere MB, und bei hundert Bildern in Folge käme so
 * mehr zusammen, als ein Handy verträgt - deshalb sind nie mehr als eine
 * Handvoll gleichzeitig offen.
 */
const OFFENE_URLS_MAX = 6;
const offeneUrls: string[] = [];

function merkeUrl(url: string): void {
	offeneUrls.push(url);

	while (offeneUrls.length > OFFENE_URLS_MAX) URL.revokeObjectURL(offeneUrls.shift()!);

	// Zusätzlich zeitgesteuert, damit auch der Rest einer kurzen Auswahl wieder
	// freigegeben wird und nicht bis zum Seitenwechsel liegen bleibt.
	setTimeout(() => {
		const i = offeneUrls.indexOf(url);
		if (i === -1) return;

		offeneUrls.splice(i, 1);
		URL.revokeObjectURL(url);
	}, 30_000);
}

/**
 * Legt ein Bild im Download-Ordner des Geräts ab.
 *
 * Bewusst synchron: der Klick muss im selben Zug wie der Tipp des Gastes
 * passieren, sonst wertet der Browser ihn als nicht angefordert und blockt.
 */
export function saveBlob(blob: Blob, fileName: string): void {
	const url = URL.createObjectURL(blob);
	const anchor = document.createElement('a');

	anchor.href = url;
	anchor.download = fileName;
	anchor.rel = 'noopener';

	// Safari ignoriert Klicks auf Elemente, die nicht im Dokument hängen.
	document.body.appendChild(anchor);
	anchor.click();
	anchor.remove();

	// Nicht sofort freigeben: der Browser löst die URL zwar beim Klick auf,
	// liest den Inhalt aber erst danach - ein sofortiges revoke() liefert auf
	// manchen Browsern eine leere Datei.
	merkeUrl(url);
}
