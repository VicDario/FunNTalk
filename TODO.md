# Pending tasks

Everything here needs a human — none of it can be done from the codebase.

> **Commit this file.** It was untracked once and a `git clean` deleted it. It
> was rebuilt from the assistant's context on 2026-08-30; anything not written
> down here was lost.

**Current state (2026-08-30): both repos merged, deployed and verified live.**

- Backend — `VicDario/FunNTalk#1` merged (squashed to `8e7e0ce`), deployed to
  Cloud Run revision `funntalk-00010-jpf` in `us-central1`, image
  `gcr.io/personal-apis-vicdario/funntalk:8e7e0ce`.
- Frontend — `VicDario/fun-n-talk-front#1` merged, live on Vercel.
  `main-5GEMIY62.js` is byte-identical (sha256 `7e5bb877…`) to a local build of
  the merged source.

---

## 1. The leaked Metered credentials — resolved, with one caveat

- [x] Revoked in the Metered dashboard — **verified by hash comparison**, not by
      trust. The credentials in public history at `4e9555f` hash to
      `4236d538…` / `24adbdea…`; production now serves `72cbe26b…` /
      `9f9e293c…`. Different values, so the rotation really happened.
- [x] New API key issued and stored in Secret Manager as `Metered`. It is a
      **different credential type** from what leaked: the leak was
      `username`/`credential` TURN pairs, the secret is a 36-byte account API
      key. Confirmed valid against Metered (`200` + TURN entries).

⚠️ The leaked pairs are **still readable in the public frontend history** at
`4e9555f` — `refs/original` is empty, so that repo was never rewritten. They are
rotated and therefore dead, but they are still there.

---

## 2. Reconcile the local repo — done

- [x] Local `main` reset to `origin/main`. HEAD is now `8e7e0ce`, tree clean.

For future reference: PR #1 was **squash-merged**, so GitHub's `main` is a single
commit whose only parent is `5c80250`. All 17 commits collapsed into it. The tree
was byte-identical so no code was lost, but the per-commit history was, and
`git bisect` across that range no longer works. Use **"Create a merge commit"**
or **"Rebase and merge"** when the commit structure is worth keeping.

Also: `git fetch` over SSH does not work from an agent here — the key is
passphrase-protected and `/usr/lib/ssh/ssh-askpass` is missing, so git serves
**stale** remote-tracking refs instead of failing loudly. Use
`gh api repos/VicDario/FunNTalk/branches` to read real remote state, or fetch
over plain HTTPS (both repos are public).

---

## 3. TURN provider — configured and live

- [x] `IceServers__Metered__ApiKey` mounted from Secret Manager (`Metered:latest`)
- [ ] ⚠️ `IceServers__Metered__Subdomain` is currently **`funntalk`, which is
      wrong**. The real Metered app subdomain is **`vicdario`**. It has to be
      corrected before minting can work — see below for why the wrong value
      looks fine today
- [x] Service account `1058570323303-compute@developer.gserviceaccount.com`
      granted `roles/secretmanager.secretAccessor` on that secret only
- [x] Verified: `/ice-servers` returns 6 entries — 2 STUN and 4 TURN
      (`turn:`/`turns:` on ports 80, 443 and TCP) each carrying a username and
      credential

The key never reaches the browser and is not in `appsettings.json`.

⚠️ **The subdomain is a wildcard on one endpoint and strictly validated on the
other, which is how a wrong value hid here.**

`GET /api/v1/turn/credentials` ignores it completely —
`zzz-does-not-exist-9f3a.metered.live` with a valid API key returns `200`. So
`funntalk` was set from a doc-comment example, appeared to work, and was believed.

`POST /api/v1/turn/credential` validates it. With the correct secret key, only
**`vicdario.metered.live`** returns `200`; `funntalk` and eight other candidates
return `401`, and so does a deliberately bogus subdomain — so that test really
does discriminate.

The code uses one `Subdomain` for both calls, so the value has to be the real app
name. Nothing was broken by the wrong value only because the endpoint in use
today happens not to check it.

⚠️ **Correction — the "short-lived credentials" assumption was false.** This file
previously claimed the minted credentials are short-lived, which would limit the
damage if one were scraped from the network tab. Production disproves it: calling
`/ice-servers` twice returns the **identical** username and credential
(`72cbe26b…` both times). Metered's `/api/v1/turn/credentials` hands back the
account's *standing* credentials, not time-boxed ones.

So any visitor who opens the network tab gets a TURN credential that works until
it is **manually** rotated. That is exactly the failure mode that produced the
original leak, and keeping the API key server-side does not fix it — it only
keeps the *account* key private.

### The fix is written — it needs your `secretKey` to switch on

`GetIceServersUseCase` now mints an expiring credential per request when a secret
key is configured, and redeems the API key that credential carries. Without one
it falls back to the standing credential exactly as before **and logs a warning
saying so**, so the current deployment keeps working rather than losing TURN the
moment this ships.

```
POST https://<subdomain>.metered.live/api/v1/turn/credential?secretKey=<KEY>
     { "expiryInSeconds": 7200, "label": "<subdomain>" }
  →  { username, password, expiryInSeconds, label, apiKey }

GET  https://<subdomain>.metered.live/api/v1/turn/credentials?apiKey=<minted apiKey>
  →  the ICE array, carrying a credential that dies on its own
```

134/134 tests pass, and the new ones are mutation-proven: forcing the standing
path fails 5, pointing the mint at the wrong endpoint fails 1.

**The blocker: the secret key is a different value from the API key.** Verified
against the live API — our stored 36-byte API key is rejected with
`401 invalid secretKey`, and so is a garbage key, while the same call with no key
at all says `secretKey query parameter missing`. It only exists in the Metered
dashboard.

#### Getting the right key — there are two, and they look identical

Metered has **two** 36-character keys and the wrong one fails silently-ish:

| Key | Where | Scope | Mints? |
|---|---|---|---|
| **Secret Key** | Dashboard → **Developers** | whole account, server-only | **yes** |
| Credential API Key | "Show API Key" next to a TURN Credential | one credential, frontend-safe | no |

The first attempt at this stored a credential API key. It is a distinct value
from the original one — so it *looked* right — but it was rejected by the create
endpoint under every subdomain. The test that settles it: a credential API key
returns `200` from
`GET /api/v1/turn/credentials?apiKey=<value>`, and a secret key never would.

- [x] Secret Key copied from Dashboard → Developers and stored in
      `MeteredSecretKey`. **Verified working (2026-08-31):** it mints against
      `vicdario.metered.live`, and it is correctly *rejected* as an API key,
      which the previous 36-character value was not. It is 48 characters; a
      credential API key is 36.
- [x] IAM granted to `1058570323303-compute@developer.gserviceaccount.com`
- [x] Whole flow proven against the live provider: mint → redeem → ICE array of
      5 entries with 4 TURN, and **two mints return different usernames**
      (`4b86969e…` then `2eace1a2…`), which is the entire point of the change
- [ ] Merge this branch, then build and deploy the new image (see the
      Deployment reference below) — the running image has no `SecretKey`
      support, so setting the variable before deploying does nothing
- [ ] Then wire it up, **both variables, as complete commands**:
      ```sh
      gcloud run services update funntalk --region us-central1 \
        --update-secrets 'IceServers__Metered__SecretKey=MeteredSecretKey:latest'

      gcloud run services update funntalk --region us-central1 \
        --update-env-vars 'IceServers__Metered__Subdomain=vicdario'
      ```
- [ ] Verify: two calls to `/ice-servers` must return **different** usernames.
      Identical values mean it silently fell back to the standing path
- [ ] Once confirmed, the `Metered` secret (the old standing API key) is no
      longer used by anything. Revoke that credential in the dashboard so the
      permanent one stops working

Tradeoff taken deliberately: it mints per request rather than caching, because
`AddHttpClient<IGetIceServersUseCase, GetIceServersUseCase>` registers the use
case as **transient** — an in-memory cache would be empty on every request, and
making it stick would mean a new singleton plus a stampede lock. Per-request
minting is also strictly safer: one user's scraped credential is not another
user's. The cost is a second round trip inside the client's 5s budget.

- [ ] Watch the Metered dashboard for credential accumulation. They persist
      until expiry, so heavy traffic will pile up 2-hour objects; lower
      `CredentialTtlSeconds` or add caching if that becomes a problem

---

## 4. Shipped together — done

`feat/random-room-codes` was a **breaking change** to the SignalR contract:
`JoinRoom` no longer creates a room as a side effect. The backend alone would
have broken every existing client; the frontend alone was useless.

- [x] Both merged, deployed and verified on the same day
- [x] Production smoke test: `POST /rooms` → `{"code":"1ZS8XS"}`, participants
      lookup → `[]`, and the rate limiter returned exactly ten `200`s before
      `429` — confirming `PermitLimit = 10` end to end

What the client does now, for the record:

- Calls `POST /api/communication/rooms` to mint a code, then joins with it.
- Two entry paths on one screen: join by code, or create and receive one.
- Discriminates `"Invalid room code."` (wrong shape) from `"Room not found."`
  (shape-valid but not live). Expired and never-existed stay identical — the
  backend will not confirm a code was ever real.
- Mirrors the server's normalization: 6 characters from a 32-symbol alphabet
  excluding `I`, `L`, `O`, `U`; trimmed, upper-cased, `I`/`L` fold to `1` and
  `O` folds to `0`, so a code read aloud over a call still works.
- Persists the code in `sessionStorage` (key `funntalk.roomCode`) so a refresh
  does not lock a creator out of a room that is still alive. The username is
  deliberately **not** persisted.
- Disables the create button in flight — the endpoint is rate limited 10/min
  per IP with no queueing, so a user mashing it locks themselves out.

---

## 4b. The backend cannot run on more than one instance

`ChatRoomRepository` stores rooms in a plain `Dictionary` behind a lock
(`ChatRoomRepository.cs:13`) and is registered as a singleton
(`Extensions.cs:20`). That is **process-local memory**.

Cloud Run was running with `maxScale: 100`. Two users landing on different
containers would see different room registries, and the second one would be told
`"Room not found."` — truthfully, from that instance's point of view. This
predates the room-codes change; it was simply invisible at low traffic.

The deploy set `--max-instances=1`, which makes it correct but caps the whole app
at a single container.

- [ ] Real fix: move the room registry to a shared store and add a SignalR
      backplane (Redis) before this ever needs to scale
- [ ] Until then, do **not** raise `max-instances` — it silently breaks rooms

Two more live-config findings, deliberately left alone because they were out of
scope for the deploy:

- [ ] `timeoutSeconds: 300` caps every WebSocket connection at five minutes, so
      long calls get cut and have to reconnect. Max is 3600.
      `gcloud run services update funntalk --region us-central1 --timeout=3600`
- [ ] The service used to reference `funntalk:latest`, and `:latest` had drifted
      off the image actually running — revision `00008` was deployed on
      2026-08-30 but served the *March 2025* build. It is now pinned to the
      commit sha. Keep pinning shas; `:latest` is not a version.

---

## 5. Install the ASP.NET Core packs (quality of life)

`dotnet test` fails locally with `NETSDK1226`: the CachyOS `dotnet-sdk` package
ships without the ASP.NET Core packs, so every build has to run through a podman
container instead. Both packages are already in your repos at the version
matching your SDK.

```sh
sudo pacman -S aspnet-runtime-10.0 aspnet-targeting-pack-10.0
```

- [ ] Install, then confirm `dotnet test` runs natively

---

## 6. Confirm the deploy target runs .NET 10 — done

The solution and `FunNTalk/Dockerfile` target .NET 10
(`mcr.microsoft.com/dotnet/aspnet:10.0`).

- [x] Confirmed: the image built and runs on Cloud Run, and production answers
      requests

---

## 7. Worth a look — CORS and local development

Not a blocker, just something that will bite during local work.

`CorsExtension` allows only the two production origins:

- `https://fun-n-talk-front.vercel.app`
- `https://funntalk.vicdario.com`

Running the Angular app from `localhost` against this backend means the browser
blocks the `ice-servers` fetch, and the client silently falls back to STUN. The
same restriction already applies to SignalR, so there may already be a
workaround in place — worth confirming before debugging a "TURN doesn't work"
report that is really CORS.

---

## Deployment reference

```sh
# Build (from a checkout of the commit you want to ship)
gcloud builds submit <src> --config=cloudbuild.yaml --substitutions=_TAG=<sha>

# Deploy — always pin the sha, never :latest
gcloud run deploy funntalk --region us-central1 \
  --image gcr.io/personal-apis-vicdario/funntalk:<sha> --max-instances=1
```

The Dockerfile lives at `FunNTalk/Dockerfile` but its build context is the
**repo root**, so it needs `-f FunNTalk/Dockerfile .` — `gcloud builds submit
--tag` will not work, since that looks for a `Dockerfile` at the source root.

Project `personal-apis-vicdario` (1058570323303), service `funntalk`, region
`us-central1`, runtime SA `1058570323303-compute@developer.gserviceaccount.com`.

## Already verified — no action needed

- 127/127 tests pass (MSTest, .NET 10)
- The regression-prone fixes are mutation-checked: reverting the ICE candidate
  target fails 2 tests, reverting the rejoin eviction fails 7, and each new
  room-code assertion was confirmed to fail against deliberately broken
  production code before being accepted
- Frontend: 74/74 tests across 8 files, `ng build` / `ng lint` / `tsc --noEmit`
  all clean
- Live smoke test of both HTTP endpoints and the per-IP rate limiter
- `dotnet publish -c Release` succeeds

## Known gaps in the room-codes change

Deliberately shipped unverified, recorded here so they are not later mistaken
for oversights:

- Rate-limit enforcement on the create endpoint is declared by attribute and not
  covered by a test — there is no `WebApplicationFactory` precedent in this
  solution, and the existing `IceServersPolicy` is untested for the same reason.
  (It was, however, confirmed by hand against production: ten `200`s then `429`.)
- The reaper's `PeriodicTimer` cadence is untested; only its sweep body is.
- `ValidateOnStart()` wiring is untested; only the pure validation function is.
  Note it guards **`RoomOptions` only** — `IceServerOptions` has no validator, so
  bad ICE config degrades to STUN rather than failing startup.
- The parallel repository test uses pre-distinct keys, so genuine code collision
  under concurrency is never actually exercised.
- Frontend: `chat-room.component.ts` has no spec, so the awaited teardown is
  untested. Awaiting teardown also makes Leave latency-bound up to SignalR's 30s
  `serverTimeoutInMilliseconds` — accepted; a slow leave beats a stranded one.
- The two hub message literals are coupled to unversioned English strings,
  guarded only by a pinning test against the backend source.

## Known traps, for future reference

- The .NET configuration binder **appends** bound array items onto a property's
  existing default instead of replacing them. `IceServerOptions.StunUrls` had a
  default identical to the `appsettings.json` entry, so the endpoint returned
  the same STUN server twice. Unit tests could not catch it — they construct the
  options object directly and never touch the binder. It took running the real
  app. `RoomOptions` is scalars-only so it cannot recur there.
- NSubstitute returns `false` for an unconfigured `bool` method, so widening a
  repository method from `void` to `bool` silently breaks every test that
  substitutes it — and the failure looks like a production bug.
- A `BackgroundService` that throws stops the whole host by default on .NET 6+.
- An assertion comparing an observed value against a compile-time literal that
  an adjacent assertion already pinned cannot fail. One of those sat in this
  suite guarding a spec requirement and guarding nothing.
- `git filter-branch` leaves backup refs under `refs/original/`, and
  `git log --all` keeps finding "purged" content until those are deleted.
- Angular lazy-loads routes via `loadComponent`, so a service used only by a
  lazy page lives in a `chunk-*.js`, **not** `main.js`. Grepping only `main.js`
  to verify a deploy gives a false negative. Chunk names are mixed-case, so a
  `chunk-[A-Z0-9]+\.js` pattern matches nothing at all.
- A probe with no negative control proves nothing. Four different Metered
  subdomains all returned `200`; only testing a garbage key (`401`) and no key
  (`400`) revealed that the subdomain is simply ignored.
