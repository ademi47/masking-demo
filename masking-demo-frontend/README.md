# Field Masking Demo — Frontend (Angular 18)

Standalone Angular app. Requires the backend package running on `http://localhost:5219`.

## Prerequisites

- Node.js 18.19+ or 20+

## Run

```bash
npm install
npm start
```

Open `http://localhost:4200`.

## Backend on a different port?

Edit `proxy.conf.json` — one line:

```json
{ "/api": { "target": "http://localhost:5219", "secure": false, "changeOrigin": true } }
```

The dev-server proxy is doing double duty here: it means every call the browser makes is
same-origin and **relative** (`/api/v1/...`), so no backend hostname ever appears in
DevTools. That is the local stand-in for putting APIM or YARP in front in production.

## Demo Script

Open DevTools (**F12**) → **Network** → filter **Fetch/XHR** before you start.

1. **GET /me (masked)** — open the call's **Preview** tab:
   `"nric": "S****567D"`, `"accountNumber": "******6789"`, `"dateOfBirth": "1985"`.
   Point out the Domain column shows only `localhost:4200`.
2. **GET /unmasked-demo** — the same record before masking, side by side.
3. **GET /me as officer** — sends `X-Demo-Role: officer`. Full values return, and the API
   console prints `UNMASKED PII ACCESS`. Privileged access is allowed, never silent.
4. **GET /me encrypted** — sends `X-Demo-Role: officer` + `X-Demo-Reveal: encrypted`. Open the
   call's **Preview** tab: `"nric"` is an AES-256-GCM ciphertext blob, not a value. Now look at
   the page itself (or DevTools → **Elements**) — the real NRIC is right there in the DOM.
   `CryptoService` decrypted it with the Web Crypto API before Angular ever rendered it.
   Network and Elements tell two different stories on purpose.
5. **PUT masked NRIC back** — returns **400**. Without this guard, a client that round-trips
   the response DTO writes asterisks over a real NRIC.
6. **Download CSV export** — masked too, because that path calls `Mask.Apply` directly.
   Exports and log sinks bypass the JSON serialiser entirely.

**Verification:** right-click the Network panel → *Copy all as HAR*, then grep for
`[STFG][0-9]{7}[A-Z]`. Zero hits outside the demo endpoint means Phase 1 is clean.
Run the same grep against your log sink — that is usually where the first surprise is.

## Files

| File | Role |
|---|---|
| `src/app/api-routes.ts` | Single place that knows API paths — all relative |
| `src/app/member.service.ts` | HTTP calls. No masking logic — the server decides. |
| `src/app/crypto.service.ts` | Decrypts AES-GCM field blobs via the Web Crypto API |
| `src/app/models.ts` | `MASKED_FIELDS` — the one list of which fields may arrive encrypted |
| `src/app/app.component.ts` | Demo UI |
| `proxy.conf.json` | Same-origin proxy to the API |

Note `"sourceMap": false` under the production configuration in `angular.json`. Source maps
in a prod build hand an attacker your entire service layer in readable TypeScript.

## Adapting to CPF

The component reads `data.nric` and renders it. That is the whole point — the client never
knew the value was masked, so wiring this into a real Angular app means changing nothing on
the frontend at all.

The encrypted-reveal path is the one place the frontend *does* know something's going on:
`CryptoService` holds a hardcoded AES key that must match the backend's
`Masking:DemoEncryptionKeyBase64`. That's a demo simplification, not a production design —
before adapting this, replace it with a short-lived per-session key issued over an
authenticated channel (see the backend README's "Adapting to CPF"), and update
`CryptoService` to fetch/rotate it instead of importing a constant.
