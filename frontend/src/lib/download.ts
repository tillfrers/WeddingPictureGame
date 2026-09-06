/**
 * Zwei Wege, ein Bild aufs Gerät zu bekommen.
 *
 * Auf dem Handy ist das Teilen-Menü der einzige Weg, der die Fotos wirklich in
 * der Galerie ablegt: iOS legt Downloads in "Dateien" statt in "Fotos", und
 * Android fragt je nach Browser vorher nach dem Speicherort. Über
 * `navigator.share` reicht dagegen ein Tipp auf "Bilder sichern".
 *
 * Am Rechner ist es umgekehrt - dort ist der Download in den Ordner das
 * Erwartete und ein Teilen-Dialog die Überraschung. Deshalb entscheidet
 * `kannTeilen()`, welcher Weg genommen wird.
 */

/**
 * Chromium nimmt höchstens 10 Dateien und 50 MB pro Aufruf entgegen; iOS kann
 * mehr, aber ein gemeinsamer Stapel spart eine Sonderbehandlung. Darum wird in
 * Häppchen geteilt - knapp unter dem Limit, damit Metadaten nicht drüberkippen.
 */
export const STAPEL_MAX_DATEIEN = 10;
export const STAPEL_MAX_BYTES = 45 * 1024 * 1024;

/**
 * Ob die Bilder über das Teilen-Menü des Geräts gesichert werden können.
 *
 * Firefox kennt `canShare`, unterstützt aber keine Dateien - das merkt man erst,
 * wenn man mit einer echten Datei nachfragt. Deshalb die Probe statt einer
 * reinen Existenzprüfung.
 */
export function kannTeilen(): boolean {
	if (typeof navigator === 'undefined') return false;
	if (typeof navigator.share !== 'function' || typeof navigator.canShare !== 'function')
		return false;

	// Nur auf Touch-Geräten: am Rechner will man die Bilder im Download-Ordner,
	// nicht im Teilen-Dialog des Betriebssystems.
	if (!window.matchMedia?.('(pointer: coarse)').matches) return false;

	try {
		const probe = new File(['x'], 'probe.jpg', { type: 'image/jpeg' });
		return navigator.canShare({ files: [probe] });
	} catch {
		return false;
	}
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
 * Legt ein Bild im Download-Ordner des Geräts ab - der Weg für alles, was kein
 * Teilen-Menü anbietet (Rechner, Firefox).
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
