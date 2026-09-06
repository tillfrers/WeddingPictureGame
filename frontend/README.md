# Hochzeits-Fotobox – Frontend

SvelteKit-SPA (Svelte 5, TypeScript) für die mobile Foto-Gallery. Jeder Tisch
hat einen QR-Code, der auf `/<TISCHCODE>` zeigt (z. B. `/9E79`).

## Setup

```bash
npm install
cp .env.example .env   # VITE_API_TARGET auf den lokalen Backend-Port setzen
npm run dev             # http://<lan-ip>:5173/<tisch>
```

`npm run dev` bindet mit `--host`, damit das Handy im selben WLAN den
Dev-Server erreicht. Der Backend-Port steht in
`backend/Api/Properties/launchSettings.json` (aktuell `5021`).

## Build & Deployment

```bash
npm run build
```

Die Static-Dateien landen in `build/`. Für den Produktivbetrieb siehe
[deploy/README.md](../deploy/README.md) — dort steht der komplette Ablauf
(Rider-SFTP, Container-Stack, Nginx-Proxy-Manager, DNS).

Zum lokalen Gegentesten des Builds:

```bash
npm run preview   # Static-Build + derselbe /api-Proxy wie im Dev-Server
```

## Warum ein Proxy statt CORS?

Das Backend hat keinerlei CORS-Konfiguration. Damit der Browser alles als
same-origin sieht, proxyt Vite `/api/*`-Aufrufe serverseitig zum Backend
(`vite.config.ts`, im Dev-Server wie in `vite preview`); Ziel per
`VITE_API_TARGET`. In Produktion übernimmt diese Rolle der `wedding-ui`-nginx.

Wichtig für Deployments: **Der gebaute Client enthält keinen Hostnamen und
keinen Port**, nur relative `/api/...`-Pfade (`new ImageClient('')` in
`src/lib/api/index.ts`). `VITE_API_TARGET` wird nie ins Bundle inlined. Derselbe
Build läuft deshalb unter jeder Domain und jedem Port — ein Umzug erfordert
keinen Rebuild.

## Generierter API-Client

`src/lib/api/client.ts` ist NSwag-generiert und wird beim `dotnet build` des
Backends aus `backend/Api/nswag.json` neu erzeugt — nicht manuell bearbeiten.
`src/lib/api/index.ts` ist der handgeschriebene Wrapper drumherum
(Client-Instanz, `displayUrl`, `apiErrorMessage`).

## Bilder aufs Handy sichern

Die Gesamtgalerie (`/gallery`) kann Bilder markieren und die Originale sichern.
Dafür gibt es zwei Wege, `kannTeilen()` in `src/lib/download.ts` entscheidet:

**Teilen-Menü** (Touch-Gerät mit `navigator.canShare({files})`, also iOS ab 14,
Chrome Android ab 76, Samsung Internet ab 11). Die Bilder gehen als Dateien an
das Teilen-Menü des Betriebssystems, und "Bilder sichern" legt sie direkt in der
Foto-Galerie ab. Nötig ist dieser Weg vor allem wegen iOS: dort bricht eine
Folge programmgesteuerter Downloads nach dem ersten Bild ab, und Downloads
landen ohnehin in "Dateien" statt in "Fotos".

Chromium nimmt höchstens **10 Dateien und 50 MB** pro Aufruf entgegen, deshalb
läuft das Sichern in Stapeln (`STAPEL_MAX_DATEIEN` / `STAPEL_MAX_BYTES`). Ein
Bild, das den laufenden Stapel sprengen würde, wird nicht neu geholt, sondern
als Übertrag in den nächsten übernommen. Jeder Stapel braucht einen eigenen
Tipp: `navigator.share()` verlangt eine frische Nutzeraktion, nach einem `await`
ist sie verbraucht.

**Download** (Rechner und alles ohne Datei-Teilen). Jedes Bild wird einzeln über
einen `<a download>` mit Blob-URL abgelegt. Weil eine offene Objekt-URL das
ganze Bild im Speicher hält und ein Original mehrere MB wiegt, sind nie mehr als
`OFFENE_URLS_MAX` gleichzeitig offen.

**Firefox** unterstützt das Teilen von Dateien nicht (weder Desktop noch
Android) und fällt deshalb immer auf den Download zurück. Fragt Firefox dabei
jedes Mal nach dem Speicherort, ist das die Einstellung
*Einstellungen → Downloads → Immer nachfragen* — eine Webseite kann sie nicht
übergehen.
