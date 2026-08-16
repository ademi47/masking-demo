# Field Masking Demo — Backend (.NET 8)

Standalone API. Runs on its own; the Angular package is optional (use `requests.http`
or Swagger if you only want to see the payloads).

## Prerequisites

- .NET 8 SDK

## Run

```bash
dotnet restore
dotnet run --project MaskingDemo.Api
```

Listens on `http://localhost:5219`. Swagger at `/swagger` (Development only — it is
deliberately disabled outside Development, since it publishes the whole API surface).

Quick check:

```bash
curl http://localhost:5219/api/v1/members/me
# "nric":"S****567D"  "accountNumber":"******6789"  "dateOfBirth":"1985"

curl -H "X-Demo-Role: officer" http://localhost:5219/api/v1/members/me
# full values + an UNMASKED PII ACCESS line in the console

curl -H "X-Demo-Role: officer" -H "X-Demo-Reveal: encrypted" http://localhost:5219/api/v1/members/me
# "nric":"<base64 nonce>:<base64 ciphertext+tag>" - AES-256-GCM, decrypted client-side
```

## How it works

`MaskingModifier` hooks `System.Text.Json` contract metadata, so any property carrying
`[Mask]` gets a masking converter automatically. That attribute is the only
masking-aware code in the application:

```csharp
[Mask(MaskKind.Nric)] public string Nric { get; init; }
```

Masking is about **who** may see a value; on top of that, `X-Demo-Reveal: encrypted`
controls **how** it's sent to someone already authorised (`X-Demo-Role: officer`) to see
it — plaintext, or an AES-256-GCM ciphertext blob the Angular app decrypts client-side via
the Web Crypto API before writing it into the DOM. Same caller privilege either way; only
the wire representation changes. This keeps PII out of the Network tab / HAR exports /
proxy and server logs, at the cost of a static demo key shared between this API's config
and the frontend bundle — see "Adapting to CPF" below.

| File | Role |
|---|---|
| `Masking/MaskingModifier.cs` | Attaches converters at contract-build time |
| `Masking/Mask.cs` | Pure mask functions — reused by CSV export and logging |
| `Masking/MaskingPolicy.cs` | Who may see unmasked values, and in what form (`RevealMode`) |
| `Masking/FieldCipher.cs` | AES-256-GCM encrypt for the `encrypted` reveal mode |
| `Masking/RejectMaskedValuesFilter.cs` | Blocks inbound payloads containing `*` |
| `Middleware/UnmaskedAccessAuditMiddleware.cs` | Audit line on every bypass |
| `Models/Member.cs` | Domain entity — always holds REAL values |
| `Dtos/MemberDto.cs` | Response DTO — the only place `[Mask]` appears |

## API Reference

| Method | Route | Notes |
|---|---|---|
| GET | `/api/v1/members/me` | Masked profile |
| GET | `/api/v1/members/me/unmasked-demo` | **Demo only** — raw entity. Delete before deploying. |
| GET | `/api/v1/members/me/export` | CSV, masked via `Mask.Apply` |
| PUT | `/api/v1/members/me/profile` | Safe write — request DTO has no NRIC field |
| PUT | `/api/v1/members/me/profile-unsafe-demo` | **Demo only** — returns 400 via the guard |

Header `X-Demo-Role: officer` bypasses masking on any GET. Adding `X-Demo-Reveal: encrypted`
alongside it swaps the plaintext for an AES-256-GCM ciphertext blob per field instead.

## Adapting to CPF

1. **Replace the demo role header.** `MaskingPolicy.CanViewUnmasked` reads a header purely
   so the behaviour is clickable. Swap in `CanViewUnmaskedProduction`, which checks a claim
   on the validated JWT.
2. **Replace the static encryption key.** `AesGcmFieldCipher` reads one shared AES key from
   config, and the exact same key is hardcoded in the frontend's `crypto.service.ts`. That's
   fine for a demo — it still keeps PII out of the wire and off logs — but it is not a secret
   from the authorised viewer's own browser (Sources/Console can find it). Production should
   issue short-lived per-session keys via an authenticated exchange, or envelope-encrypt with
   a KMS, and rotate them.
3. **Replace `CurrentMemberId`.** Hardcoded in the controller. In production it comes from
   the JWT `sub` claim — never a header, route parameter or query string. An NRIC in a query
   string lands in gateway logs, App Insights and browser history in plaintext, which undoes
   the masking entirely.
4. **Delete both `-demo` endpoints.**
5. **Confirm the mask formats** against any existing CPF standard for partial NRIC display
   before finalising — worth checking with whoever owns data governance rather than assuming.
6. **Route the audit log to a tamper-evident sink**, not `ILogger`.

### Extending

- New masked field: add `[Mask(MaskKind.X)]` to the DTO property. It will automatically pick
  up all three reveal modes — nothing else needed on the backend. On the frontend, also add
  the field name to `MASKED_FIELDS` in `models.ts` so the encrypted-reveal demo button knows
  to decrypt it.
- New mask type: add a `MaskKind` member and a branch in `Mask.Apply`.
- Non-string types: the modifier only attaches to `string` properties. A masked `DateOnly`
  or `decimal` needs its own `JsonConverter<T>` registered the same way.

### Known gaps

In-memory store, no auth, no tests. Production work needs xUnit coverage on `Mask.Apply`
(boundary lengths, nulls, short values) and an integration test asserting no unmasked
NRIC pattern appears in any serialised response.
