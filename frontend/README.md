# Hochzeits-Fotobox – Frontend

SvelteKit-SPA (Svelte 5, TypeScript) für die mobile Foto-Gallery. Jeder Tisch
hat einen QR-Code, der auf `/<TISCHCODE>` zeigt (z. B. `/9E79`).

## Setup

```bash
npm install
cp .env.example .env   # VITE_API_TARGET ggf. anpassen
npm run dev             # http://<lan-ip>:5173/<tisch>
```

`npm run dev` bindet mit `--host`, damit das Handy im selben WLAN den
Dev-Server erreicht.

## Build & Ausliefern

```bash
npm run build
npm run preview   # dient den Static-Build + Proxy, --host für LAN-Zugriff
```

Die Static-Dateien landen in `build/`. Für einen echten Betrieb reicht ein
beliebiger Static-Webserver mit SPA-Fallback auf `index.html`, kombiniert mit
einem Reverse-Proxy für `/api` auf das Backend (siehe unten).

## Warum ein Proxy statt CORS?

Das Backend setzt keine CORS-Header. Damit der Browser trotzdem alles als
same-origin sieht, proxyt Vite `/api/*`-Aufrufe serverseitig zum Backend
(`vite.config.ts`, sowohl im Dev-Server als auch in `vite preview`). Ziel ist
per `VITE_API_TARGET` konfigurierbar (Standard `http://localhost:5037`).

Für einen dauerhaften Produktivbetrieb außerhalb von `vite preview` braucht es
stattdessen einen Reverse-Proxy (nginx/Caddy/…), der `/api` an das Backend
weiterleitet und den Rest als statische SPA-Dateien ausliefert.

## Bekannte Backend-Eigenheit, die im Frontend umschifft wird

Fehlerantworten (`BadRequest("...")`) liefern einen rohen JSON-String statt
eines `ProblemDetails`-Objekts. Ein kleiner `fetch`-Wrapper in
`src/lib/api/index.ts` wandelt das vor der generierten Client-Deserialisierung
in ein kompatibles Objekt um, damit die eigentliche Fehlermeldung nicht
verloren geht (`ProblemDetails.fromJS` im generierten Client verwirft
nicht-objektartige Bodies sonst stillschweigend).

Zwei weitere Backend-Bugs (falsche `thumbnailUrl`-Route, `UploadResultDto` als
nicht instanziierbare abstrakte Klasse im generierten Client) wurden direkt im
Backend behoben.

`src/lib/api/client.ts` ist NSwag-generiert (`backend/Api/nswag.json`) und
sollte nicht manuell bearbeitet werden.
