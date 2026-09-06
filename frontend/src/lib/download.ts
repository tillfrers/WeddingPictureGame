/**
 * Legt ein heruntergeladenes Bild im Download-Ordner des Geräts ab.
 *
 * Ein Blob plus `<a download>` ist der einzige Weg, der auf Android-Chrome wie
 * auf iOS-Safari funktioniert: Die Datei kommt mit dem Auth-Cookie über fetch
 * herein, und der Anker gibt sie an die Speichern-Funktion des Browsers weiter.
 * Auf dem iPhone landet sie damit in "Dateien", auf Android in "Downloads".
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

	// Nicht sofort freigeben: iOS liest den Blob erst nach dem Klick aus, ein
	// zu frühes revoke() liefert dort eine leere Datei.
	setTimeout(() => URL.revokeObjectURL(url), 60_000);
}
