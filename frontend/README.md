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
Dafür gibt es drei Wege, `sicherWeg()` in `src/lib/download.ts` entscheidet.

**`teilen`** — Touch-Gerät mit `navigator.canShare({files})`, also iOS ab 14,
Chrome Android ab 76, Samsung Internet ab 11. Die Bilder gehen als Dateien ans
Teilen-Menü des Betriebssystems, und "Bilder sichern" legt sie direkt in der
Foto-Galerie ab. Nötig ist dieser Weg vor allem wegen iOS: dort bricht eine
Folge programmgesteuerter Downloads nach dem ersten Bild ab, und Downloads
landen ohnehin in "Dateien" statt in "Fotos".

Chromium nimmt höchstens **10 Dateien und 50 MB** pro Aufruf entgegen, deshalb
läuft das Sichern in Stapeln (`STAPEL_MAX_DATEIEN` / `STAPEL_MAX_BYTES`). Ein
Bild, das den laufenden Stapel sprengen würde, wird nicht neu geholt, sondern
als Übertrag in den nächsten übernommen. Jeder Stapel braucht einen eigenen
Tipp: `navigator.share()` verlangt eine frische Nutzeraktion, nach einem `await`
ist sie verbraucht.

**`einzeln`** — Touch-Gerät ohne Datei-Teilen, praktisch also Firefox für
Android und In-App-Browser. Dort zeigt der Browser einen modalen
Speichern-Dialog und verträgt immer nur einen: Klicks, die währenddessen kommen,
bleiben in der Warteschlange hängen und tauchen erst wieder auf, wenn die App
aus dem Hintergrund zurückkehrt — es wird also nur ein Bild gespeichert, obwohl
die Anzeige alle als erledigt meldet. Deshalb wird hier pro Tipp genau ein Bild
gespeichert, und immer nur eins im Voraus geladen.

**`auto`** — Rechner. Alle Bilder laufen nacheinander über `<a download>` mit
Blob-URL durch, ohne Zutun. Weil eine offene Objekt-URL das ganze Bild im
Speicher hält und ein Original mehrere MB wiegt, sind nie mehr als
`OFFENE_URLS_MAX` gleichzeitig offen.

Fragt Firefox bei jedem Bild nach dem Speicherort, ist das die Einstellung
*Einstellungen → Downloads → Immer nachfragen* — eine Webseite kann sie nicht
übergehen.

## Nur ein Auth-Cookie

Das Backend führt einen einzigen Cookie (`wpg_auth`), jedes `redeem`
überschreibt ihn. Wer den Einladungslink offen hat und danach den QR-Code am
Tisch scannt, verlöre damit den Zugang zur Gesamtgalerie. Deshalb fragt
`+layout.ts` vor dem Einlösen eines Tisch-Tokens nach, ob der weitergehende
Zugang schon steht, und überspringt es dann — er deckt die Tisch-Endpunkte
ohnehin mit ab.
