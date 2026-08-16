# Speaking Transcript — Field-Level Masking Walkthrough

**Runtime:** ~32 minutes for 14 slides including the live demo, plus questions.
**Short version:** cut slides 6 and 11 and shorten the demo to steps 1, 3 and 4 → ~20 minutes.

Stage directions are in square brackets. Everything else is meant to be said roughly as written, though it should sound like you rather than like a script — read it once, then talk from the bold lines.

---

## Slide 1 — Title *(~1 min)*

Thanks for making time. This is a walkthrough of a change I've built and want a second opinion on before it goes anywhere near a real service.

The starting point was a question that came up a while back: can we stop people seeing our API calls in the browser's network tab. The honest answer turned out to be no — but working out *why* not led somewhere more useful, and that's what we ended up building.

Two layers. The first redacts sensitive fields before the payload leaves the API. The second makes the route surface impossible to enumerate. Both are small changes, and neither touches a controller.

There's a runnable demo at the end, so if something doesn't make sense on a slide, hold it and we'll look at the real thing.

---

## Slide 2 — The ask *(~3 min)*

The original request was: hide our API calls so nobody can see them when they press F12.

**You can't.** The browser is the client. It has to resolve the URL to make the request, so it has to know the URL. Anything Angular sends, DevTools shows.

And encryption doesn't rescue it either — that's the part worth being clear on. If we encrypt the payload, the key has to reach the browser too. Anyone can set a breakpoint in the interceptor and read the plaintext before it's ever encrypted, or just log it. We'd have moved the disclosure from the Network tab to the Sources tab.

[*Pause here. This lands better if you let it sit for a second.*]

If it helps calibrate — open DevTools on Facebook, Gmail, or your own bank. Every API call is right there. Nobody has solved this, because it isn't solvable.

So we changed the goal. Not *hidden* — **visible but useless**. A reviewer opening F12 still sees a working API. What they get out of it is already redacted, and replaying the call gains them nothing.

That reframe is the whole design. Everything after this slide follows from it.

---

## Slide 3 — Before and after *(~2 min)*

This is the same endpoint, the same member, the same Preview tab in DevTools.

On the left, what we ship today. Full NRIC, full account number, exact date of birth, full email, full mobile. All of that is sitting in the browser, in the browser's memory, in the HAR file if anyone exports one, and in any proxy along the path that logs bodies.

On the right, after the change. Partial NRIC, last four of the account, birth year only, masked email and mobile.

The important bit is what *didn't* change. Look at `contributions` — untouched. This is per-field, not per-payload. [*Point at the three cards along the bottom.*]

That matters more than it looks. Because we're not encrypting anything, the WAF can still inspect the response, caching still works, App Insights still captures something legible, and when someone reports a wrong balance at 2am you can still read the payload. Those are real operational costs we'd have paid with whole-body encryption, and we're not paying them.

And the full value never leaves the datacentre. There's no key in the browser, so there's nothing to recover.

---

## Slide 4 — Where masking happens *(~3 min)*

Reading right to left is the response travelling out.

The domain and the database hold real values. Always. The API boundary is where masking happens. From there outward — through the gateway, into Angular — everything is already redacted.

**Why not mask earlier?** This is the question I'd expect in a design review, so let me answer it first.

The domain needs real values. You need the actual NRIC to look up the member, to join records, to call a downstream service, to write an audit entry. If you mask on the way *in*, or in the repository, or on the entity, you'll find yourself unable to match your own data — and you'll discover it in production, in an edge case, weeks later.

The boundary is the only place in the stack where "this data is leaving our perimeter" is actually true. That's why the masking lives there and nowhere else.

Practical version of that rule: `Member` never gets a `[Mask]` attribute. `MemberDto` does. If you ever see masking logic inside a service or a repository in code review, that's a regression — say so.

---

## Slide 5 — The entire developer-facing API *(~2 min)*

This is the whole thing you need to know to use it.

One attribute on a DTO property. That's it. `[Mask(MaskKind.Nric)]`, `[Mask(MaskKind.Account)]`. No masking logic in the controller, none in the service, none in Angular.

[*Walk the right-hand column.*]

Adding a masked field is one attribute. Adding a new *format* — say we need a masked postal code — is one enum member and one branch in `Mask.Apply`. Controllers stay thin, which is what we want anyway.

And on the frontend: the Angular component reads `data.nric` and renders it. It never knew the value was masked. That's deliberate. If wiring this into a real Angular app required frontend changes, we'd have built it wrong.

---

## Slide 6 — How the attribute gets picked up *(~3 min)*

*[Optional slide — skip if the room is more architecture than implementation.]*

This is the only clever part of the change. Everything else is plumbing.

System.Text.Json in .NET 8 lets you hook the contract metadata — the description it builds of how to serialise a type. We register a modifier that walks each type's properties, looks for our attribute, and attaches a converter where it finds one.

Three things worth knowing about it. [*Bottom cards.*]

It runs **once per type**, at contract-build time, and the result is cached. There's no per-request reflection, so the performance cost is effectively zero.

It only attaches to **string properties**. If we later want a masked `DateOnly` or a masked `decimal`, that needs its own `JsonConverter<T>` registered the same way. Right now the date of birth is a string in the DTO for exactly that reason — worth knowing so nobody is surprised.

And it's registered in **one place**, `services.AddFieldMasking()`, following our convention of keeping feature registration out of `Program.cs`.

---

## Slide 7 — The trap *(~3 min)*

If you remember one slide from this session, make it this one. This is how field masking corrupts production data.

The client receives a DTO with `"nric": "S****567D"`. The user edits their email address, and the app PUTs the whole object back — which is a completely normal thing for an Angular form to do.

**We just wrote asterisks over a real NRIC.**

And here's why it's easy to miss. [*Point at the bottom-left card.*] Nothing fails. The PUT returns 200. The UI re-renders the masked value it just sent, so the screen looks correct. The record looks fine until someone tries to match it downstream — possibly weeks later, possibly in a batch job at month end.

Three defences, in order.

**One — separate request DTOs.** `UpdateProfileRequest` has no `Nric` property at all. Not ignored, not read-only. Absent. This is the primary defence and it's the one that actually matters.

**Two — identity from the token.** The member ID comes from the JWT `sub` claim. Never the body, never a route parameter, never a query string. Worth stressing the query string specifically: an NRIC in a query string ends up in gateway access logs, in App Insights, and in browser history, all in plaintext. That single habit undoes every other thing on these slides.

**Three — a filter that rejects any inbound payload containing an asterisk**, with a 400. That's a safety net, not the plan. If it ever fires in production, something upstream is wrong and we should go find it.

---

## Slide 8 — Three paths that bypass the serialiser *(~2.5 min)*

Masking the JSON is not the same as masking the data. There are at least three paths out of the system that never touch System.Text.Json.

**Log sinks.** `ILogger` and Serilog write straight from entities. And logs are retained longer than payloads — so an unmasked NRIC here is the same disclosure, kept for longer, in a system that usually has broader read access. Fix is either calling `Mask.Apply` at the log site or a destructuring policy.

**Exports.** CSV and Excel generation reads the entity and writes text. In the demo, `ExportCsv` calls `Mask.Apply` explicitly for exactly this reason.

**Error responses.** Validation messages love to echo the input back — "S1234567D is not a valid member." Worth checking your ProblemDetails and your model-binding messages.

[*Beat.*] In my experience the log sink is where the first surprise turns up. So when we verify this — and I'll show the check in a few slides — we run it against the logs, not just the wire.

---

## Slide 9 — Roles that need the full value *(~2 min)*

Some people legitimately need to see the real NRIC. An officer handling a case, for instance. So this isn't all-or-nothing.

Three principles.

Gate on a **specific permission claim** from the validated JWT — `member.pii.read` — not on a role name that drifts over time, and not on anything the client can set.

**Audit is mandatory.** Every bypass writes an entry: who, what path, correlation ID, source IP. In production that goes to a tamper-evident sink with its own retention, not the application log. Privileged access is allowed. It is never silent.

And the **default is masked**. The bypass is opt-in per request. If the claim check throws, or the HTTP context is missing, you get the masked value. Failure mode points the safe way.

One caveat on the demo: it uses an `X-Demo-Role` header so the behaviour is clickable in a browser. That header must not survive into any real deployment, and the code has the production version sitting right next to it.

---

## Slide 10 — Layer 2: opaque route names *(~2.5 min)*

That's the payload dealt with. This layer is about the URL.

Today a route reads `/api/v1/members/{nric}/contributions`. It tells you the resource model, the versioning scheme, and roughly what else probably exists. After, it's `/api/q/7f3a9c21` — an identifier that means nothing.

I want to be precise about what this buys, because it's easy to oversell.

**What it stops:** automated discovery. Scanners run wordlists — `/api/users`, `/api/admin`, `/api/v1/export` — and against opaque routes every one of them 404s. That's a real reduction in noise, and a real reduction in the odds that some forgotten endpoint gets found.

**What it does not stop:** anyone actually looking at our app. The full map ships in the Angular bundle. Open Sources, search for `/api/q/`, and you have every route in about thirty seconds.

So in the design doc I'd call this **non-enumerable routing**, not a security control. Obscurity described as a control is a finding in itself, and I'd rather we describe it accurately than have a reviewer catch it.

For what it's worth, the big platforms end up here too — persisted GraphQL queries at Facebook and X show an opaque ID instead of a named route. But they got there for typing and performance reasons; the security effect is a side benefit. Same for us.

---

## Slide 11 — Where the map lives *(~2.5 min)*

*[Optional slide — skip for a non-implementation audience.]*

The mapping goes at the gateway, not on the controllers. YARP matches the opaque path and transforms it back to the real one before it reaches the service. No .NET code changes, and rotating an ID is a config change, not a redeploy.

On the Angular side, a single constants file. Readable names on the left so components stay legible, opaque strings as the values.

Three rules. [*Bottom cards.*]

**Not on the controllers.** If you put `[HttpGet("~/api/q/7f3a9c21")]` on a method called `GetContributions`, the opaque ID sits right next to its own answer, and six months later nobody can find anything.

**Keep the dashboards readable.** We attach an `X-Op-Name` header at the gateway and log that instead of the public path. Otherwise App Insights becomes a wall of `/api/q/7f3a9c21` and your first 2am incident is considerably less fun than it needs to be.

**Block the internal paths.** If the readable route is still reachable through the gateway alongside the opaque one, we've bought exactly nothing.

One thing to resist while we're here: decoy endpoints that return fake data. It's tempting, but monitoring can't tell decoy traffic from a real incident, a pen-test team burns days chasing them, and "the system deliberately returns false data under some conditions" is an awkward sentence in an audit report.

---

## Slide 12 — Live demo *(~6 min)*

[*Switch to the browser. Have both terminals already running and DevTools already open, filtered to Fetch/XHR. Zoom the browser to ~125% so the room can read the Preview tab.*]

Backend on 5219, Angular on 4200.

**One — the masked call.** [*Click.*] Here's the request. Note the Domain column: `localhost:4200`. Everything is same-origin and relative, so the backend host never appears — that's the local stand-in for putting APIM in front in production. And the path is opaque. Now the Preview tab: masked NRIC, masked account, year only.

**Two — the before payload.** [*Click.*] Same record, straight off the entity. This endpoint exists only for this comparison and gets deleted before anything ships. Side by side, that's the difference.

**Three — as a privileged role.** [*Click, then switch to the API console.*] Full values come back — and here in the console, `UNMASKED PII ACCESS`, with the path and correlation ID. Allowed, but never silent.

**Four — the trap, live.** [*Click.*] This sends `S****567D` back to the server. Four hundred. The asterisks never reach the database.

**Five — the CSV export.** [*Click.*] Masked as well, because that path calls `Mask.Apply` directly rather than going through the serialiser.

**And the verification step** — this is the bit I'd want us to do on any real implementation. Right-click the Network panel, Copy all as HAR, and grep it for the NRIC pattern. Zero hits outside the demo endpoint means we're clean. Then run the same grep against the log sink, which is where I'd expect the first surprise.

---

## Slide 13 — What this does and does not buy us *(~2 min)*

Quick honest summary, so nobody oversells this in a design review.

**Closed:** full NRIC, account number, DOB, email and mobile never reach the browser. Replaying a captured request returns redacted data. Backend topology is invisible and scanner wordlists get 404s. It's aligned with what PDPC guidance expects — partial NRIC is the default for display, so this isn't something we invented. And our WAF, caching and APM all keep working, because nothing is encrypted.

**Still open:** the route IDs ship in the JS bundle. The JWT is readable and decodes to plaintext claims. There's no request signing, so a captured call can still be replayed by a bot. And — the big one — **none of this substitutes for server-side authorization on every endpoint.**

That last point is where I'd want our next effort to go. An attacker knowing our URL should be worth nothing. That's authorization work, not masking work, and it's a bigger job than this.

Also worth saying plainly: the demo has an in-memory store, no auth and no tests. It's a demo, not the implementation.

---

## Slide 14 — Next *(~2 min)*

In priority order.

**One, ship Phase 1** — the attribute, the modifier, the write-path guard. Then run both greps before anyone calls it done.

**Two, confirm the mask formats.** I don't want to invent a house style for partial NRIC if CPF already has one. Someone needs to check with whoever owns data governance — including whether any role legitimately needs unmasked reads, because that changes the converter.

**Three, add the tests.** xUnit on `Mask.Apply` for boundary lengths, nulls and short values. And an integration test that asserts no unmasked NRIC pattern appears in any serialised response — that's the one that catches the field somebody adds next year and forgets to annotate.

**Four, map the routes at the gateway.** Config-only. Block the internal paths at the same time, and log `X-Op-Name` so the dashboards survive.

**Five, decide on Phase 2** — field-level *encryption*, for values the browser genuinely needs in full or that must stay opaque to an inspecting proxy. My read is that's two or three fields, not everything. But that's a decision for us, not for me.

**And the open question I'd like to leave with you:** who owns the route-ID map, and what's our rotation policy when one leaks?

[*Stop there. Take questions.*]

---

## Likely questions — prepared answers

**"Why not just encrypt the whole response?"**
The key has to reach the browser, so it doesn't defeat a human with DevTools — it only defeats bots and inspecting proxies. And the costs are real: the WAF can't inspect responses, APM goes dark, caching dies, and debugging gets materially harder. Masking gets most of the benefit at a fraction of the operational cost. If a specific requirement names payload encryption I've got a design for it, but I wouldn't lead with it.

**"Won't the masking break our integration partners?"**
Only if they consume the same DTOs as the browser. Server-to-server integrations should be on their own DTOs without the attribute, or gated on the permission claim. Worth auditing before we ship.

**"What about performance?"**
Contract metadata is built once per type and cached. Per-request cost is a string operation per masked field. Negligible.

**"Isn't the opaque routing just security through obscurity?"**
Yes, and I'd describe it exactly that way. It stops automated enumeration and nothing else. It's cheap enough to be worth doing and dishonest to call a control.

**"Can't someone just read the masked value off the screen anyway?"**
The user seeing their own data is fine — that's not the threat. The threat is the full value sitting in HAR exports, proxy logs, browser history and APM traces, and being available to anyone who captures traffic rather than only to the account holder.

**"What if we need the full NRIC in the UI for a specific screen?"**
That's the permission-claim path on slide 9, plus an audit entry per read. If a whole screen needs it routinely, that's worth a conversation about whether the screen actually needs it — usually the answer is that it needs to *match* on it, which the server can do without ever sending it.
