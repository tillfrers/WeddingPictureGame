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
