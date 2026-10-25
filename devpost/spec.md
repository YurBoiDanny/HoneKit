---
doc: spec
status: approved
---

# HoneKit — Technical Spec

## How This Works, In Plain Language

HoneKit is one app made of six programs that talk to each other over HTTP, plus a launcher that
starts them all.

- **Content** holds the curriculum. Decks, cards, the word choices, the answer explanations, the
  target frame for each framing scenario. If it's something a leader authored, it lives here.
- **Progress** holds what people have done. Who answered which card, whether they were right, when
  they'll see it again, and how much of each skill they've completed.
- **Roster & Bags** holds the people. Names, roles, bag colour and style, and the crew's health
  read — the team bag and the bag wall.
- **Requirements** turns a meeting brief into a list of skills the meeting needs. This is the one
  place a model is called.
- **Recommendations** turns requirements plus progress into a suggested roster, and explains it.
  Pure rules — no model.
- **Frontend** is the React app in the browser. It talks to the five services directly.
- **AppHost** is the launcher. One command starts all six, prints their addresses, and shows a
  dashboard with logs and traces.

Think of it as five spreadsheets that each own their own rows, with a web app in front. The
Recommendations service cannot edit the Progress spreadsheet; it reads it. That rule is the whole
point of splitting them: each service's data has exactly one owner, and the boundaries are the
shapes you'd draw if you were explaining the team's work to someone.

**Why not one program?** Because the goal is to practise distributed architecture at small scale.
One program would prove the app works; five prove you can design boundaries, contracts, and
failure behaviour — and they give five independently buildable pieces, which is what makes the
multi-agent orchestration practice real rather than theatrical.

## The Core Journey Through the System

Traces `prd.md > The Core Journey`, steps 1–16.

| Step | What happens | Where it lives |
|---|---|---|
| 1 | Leader lands on the team bag | Frontend → Roster & Bags `GET /teams/camera/bag` |
| 2–3 | Roster list, then the bag wall | Roster & Bags `GET /teams/camera` + `GET /teams/camera/members` |
| 4 | Switch to their own bag | Same nav; Roster & Bags `GET /members/{id}/bag` |
| 5 | Open the Camera deck | Frontend → Content `GET /decks?role=camera` |
| 6 | Answer a card | Progress `POST /sessions/{id}/attempts` → grade returned |
| 7 | Finish the deck | Progress `POST /sessions/{id}/finish` → score + pass |
| 8 | Tool lands in the bag | Frontend reads `GET /members/{id}/bag`; a newly earned tool has `acknowledged: false` and animates in, then `POST /members/{id}/bag/acknowledge` |
| 9 | Personalize | Roster & Bags `PATCH /members/{id}/bag` |
| 10 | Open a teammate's bag | Roster & Bags, read-only view |
| 11 | Return later | SQLite files on disk; every service re-reads on start |
| 12 | Upload the brief | Frontend → Requirements `POST /requirements` (file) |
| 13 | Edit the parse | Requirements `PUT /requirements/{id}` |
| 14 | Ask for a roster | Frontend → Recommendations `POST /recommendations` |
| 15 | Override by hand | Client-side selection; warning computed by Recommendations `POST /recommendations/check` |
| 16 | Save the plan | Recommendations `POST /plans` |

The bag's "drop" is deliberately **derived, not pushed**. Roster & Bags computes earned tools from
Progress's completion data and marks new ones unacknowledged. Nothing has to be synchronised, and
the animation still fires at the right moment — the first time you're home after finishing.

## Stack

| Piece | Choice | Why |
|---|---|---|
| Runtime | .NET 10 (SDK 10.0.401 installed) | LTS, current |
| Services | ASP.NET Core minimal APIs | Smallest surface per service; OpenAPI is a first-class citizen |
| Persistence | EF Core 10 + one SQLite file per service | Zero infrastructure; data ownership stays visible as five files |
| Orchestration (dev) | .NET Aspire 13.6 | Inner loop: one command, dashboard, traces across services |
| Packaging | Aspire AppHost as the model → generated Docker Compose | One description of the system; compose is output, not a second source of truth. Images are ordinary OCI, so any Docker host can run them |
| Contracts | Shared `HoneKit.Contracts` project of DTO records | One definition the agents can build against, so a frontend agent never guesses a shape |
| Model access | `Microsoft.Extensions.AI` 10.11.0 (`IChatClient`) over `Microsoft.Extensions.AI.OpenAI` + `OpenAI` 2.14.0 | One interface, three providers, structured output built in |
| PDF text | PdfPig (Apache 2.0) | Only needs text out, not layout |
| Frontend | React + Vite + TypeScript (pin current stable at build) | Pointer-driven framing interaction; separate deployable |
| Data fetching | TanStack Query | Cache, retry, and loading states without hand-rolling them |
| Styling | Plain CSS with custom-property design tokens | The hand-painted direction needs hand-written CSS, not utility defaults |
| Tests | xUnit (services) + Vitest (frontend geometry helpers) | Fast, no browser needed for the rules |

Documentation: [Aspire](https://aspire.dev/) · [Microsoft.Extensions.AI](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai) ·
[structured output](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/structured-output) ·
[Ollama OpenAI compatibility](https://docs.ollama.com/api/openai-compatibility) ·
[OpenCode Zen](https://opencode.ai/docs/zen/) · [PdfPig](https://github.com/Uglytoad/PdfPig) · [Vite](https://vite.dev/)

**Unverified, check first thing in the build:** Aspire 13.6 ships both `AddProject<T>()` (the older
model) and `AddDotnetProject()` (Project V2). Confirm which the installed template emits, and pin
whichever works rather than mixing them. Also pin PdfPig's latest *stable* version — its version
numbering is unusual and the NuGet listing mixes stable and nightly builds.

## Where It Runs and How Someone Tries It

Two ways in, one model. **The AppHost is the single source of truth** — services, endpoints,
volumes, environment and dependencies are declared there, and Docker Compose is generated from it.
Aspire's own guidance is blunt on this: *"The AppHost is the deployment model, not a Compose
workload"* and *"do not hand-edit `docker-compose.yaml` as the durable fix"*. So the YAML is an
artifact, committed so it's readable and deployable without the CLI, but never edited by hand.

```bash
# one command: build images, generate compose, start the stack
aspire deploy

# or the explicit steps
aspire publish                 # → aspire-output/docker-compose.yaml + .env (no images built)
aspire do prepare-compose      # resolve env, build images
cd aspire-output && docker compose up -d

# inner loop: services as debuggable host processes, with the dashboard and traces
cd src/HoneKit.AppHost && dotnet run
```

- Published compose exposes the frontend at `http://localhost:8080`. The five services stay on the
  compose network and are reached by name, never by a published port.
- Each service keeps its SQLite file on a named volume (`content-data`, `progress-data`,
  `roster-data`, `requirements-data`, `recommendations-data`), so stopping the stack doesn't lose
  the demo's progress. `docker compose down -v` in `aspire-output/` is the reset.
- Every service has a health check; the frontend waits on them, so a cold start doesn't show broken
  pages. Use HTTP endpoints rather than HTTPS between services — the Compose publisher has a history
  of emitting unusable HTTPS service-discovery variables.
- To deploy elsewhere: build on a machine with the Aspire CLI, then copy `aspire-output/` (or push
  the images) to any Docker host and `docker compose up -d`. **The deploy host needs Docker only** —
  no Aspire CLI, no .NET SDK, no cloud account and no cluster.

**Drift guard.** A committed generated file rots the moment someone forgets to regenerate. One
command closes that hole, and it runs before every recording:

```bash
aspire publish && git diff --exit-code aspire-output/
```

**AI configuration** (`.env`, gitignored, with `.env.example` committed):

```bash
AI__PROVIDER=fixture          # fixture | openai | ollama | opencode
AI__MODEL=gpt-5-mini
AI__ENDPOINT=https://api.openai.com/v1
AI__APIKEY=
```

`fixture` is the default and needs no key: it replays a committed parse of the sample brief, so the
recorded demo never depends on a network call. Switching to a live provider is the block above and
nothing else.

One container-specific wrinkle worth knowing before it bites: **Ollama on the host is not
`localhost` from inside a container.** Use `AI__ENDPOINT=http://host.docker.internal:11434/v1` on
macOS/Windows, or add `extra_hosts: ["host.docker.internal:host-gateway"]` on Linux. Running Ollama
as its own compose service is the third option and avoids the problem entirely.

**For the demo video:** start the stack, then follow this order — leader home and team bag → bag
wall → switch to own empty bag → run the Camera Foundations deck (one fill-in-the-blank, one framing
scenario, one deliberate wrong answer) → watch the tool land → personalize → open a teammate's
bag → Meeting plan → upload the sample brief → edit a requirement → switch strategy → hand-pick an
override → save the plan. Roughly four minutes.

Submission itself needs the **video and a public GitHub repository**. Deployment is now possible
but optional: the compose file is the artifact, and `6-ship` decides whether to actually run it
somewhere.

**Honest limits of this deployment:** SQLite means one replica per service — it will not scale out,
and two containers must never share a database file. There's no auth and no TLS, so a public URL
would be a demo instance, not a production one. That's the right size for this POC, but it should
be said out loud rather than discovered later.

## Look and Feel

Carried forward from `prd.md > Look and Feel` and `scope.md > Inspiration & Identity`, translated
into tokens the build can apply.

- **Palette:** warm pastels on a soft cream ground — butter yellow, sage, dusty rose, powder blue,
  with brass as the earned accent and a deep green-teal reserved for the *honed* finish. Ink is a
  warm brown-black, never pure black. Light theme first; dark theme deepens the same hues rather
  than inventing greys. Saturation stays low: inviting, not candy.
- **Typography:** a humanist serif for headings and the bag's labels (Fraunces or Source Serif), a
  clean sans for UI and body (Inter). Large, generous headings; all-caps only for small labels.
- **Texture and shape:** hand-painted means imperfect edges. Subtle paper grain, slightly uneven
  radii, soft inner shadows on the bag, illustrations with visible brush wobble. No glassmorphism,
  no neon, no generic gradient mesh.
- **Density:** generous. This is a tool someone opens on a Sunday morning, not a dashboard they
  monitor. One idea per screen section.
- **Motion:** spring-like, 200–300 ms, used for the tool drop, the bag unzip, and the small bag
  nudge on a correct answer. Everything else is quiet. `prefers-reduced-motion` replaces the drop
  with a fade.
- **Tone of copy:** direct, warm, never cute. "Not quite — try again." "This deck needs one more
  pass." No exclamation marks in error states.
- **Accessibility floor:** WCAG 2.2 AA contrast, 44×44px targets, visible focus rings, meaning never
  carried by colour alone (honed state also gets a label).

### The bag, visually

A canvas camping pack crossed with a scout's kit bag: top flap with a buckle, two side pockets,
carabiners and a rolled mat strapped on, tools poking out the top, and **patches sewn on the front**
— one per completed skill. Drawn as layered SVG so a colour change is a token change and a honed
tool is a different layer, not a different asset. Rough, hand-inked outlines; flat fills with one
soft highlight.

Reference photographs live in `samples/art/`: `bag-02.jpg` (tactical pack with patches),
`bag-04.jpg` (canvas pack covered in sewn patches), `bag-03.jpg` (embroidered pack patch — the
motif for individual patches). They are **references, not shipped assets**.

### Framing card photography

Three stage photographs are downloaded and committed to `samples/art/`:

- `stage-01.jpg` — modern church stage, lectern at right, portrait. The primary "leader teaching at
  the lectern" scenario.
- `stage-03.jpg` — sanctuary wide shot from the seating, pulpit centred. The wide-versus-tight
  scenario.
- `stage-02.png` — auditorium with a lit stage. A third scenario if the deck grows.

**Licensing caveat, stated plainly:** these came from public search results and I could not verify
their licences. They're fine as placeholders for a local demo, but the GitHub repo is public, so
before publishing either replace them with known-licensed or self-taken photographs, or keep them
out of the repository and load them locally. This is a `6-ship` task, not a build blocker.

## Components

### Content service
Owns curriculum. Authors create decks and cards; everyone reads them. No knowledge of people.
PRD ref: `prd.md > Deck authoring`, `prd.md > The member loop`.

- `GET /api/roles/{role}/skills` — the skill catalogue (id, name, lesson count). Consumed by
  Requirements to keep the model's vocabulary closed.
- `GET /api/decks?role=camera` — deck summaries with card counts.
- `GET /api/decks/{id}` — deck with card list.
- `GET /api/cards/{id}` — one card, its type, image ref, choices or target frame, explanation, and
  per-choice wrong reasons.
- `POST /api/decks`, `PUT /api/decks/{id}/cards`, `POST /api/decks/{id}/assign` — authoring.

Seeded content: one role (`camera`), four skills — Framing, Gear, Operation, Switching — each with
two lessons; one authored deck (`Camera Foundations`, skill `framing`) with four cards.

### Progress service
Owns attempts and scheduling. Answers cards, grades them, decides what comes back when.
PRD ref: `prd.md > The member loop`, `prd.md > States and Boundaries` (persistence).

- `POST /api/sessions` — start a session for `{memberId, deckId}`; returns the queue.
- `GET /api/sessions/{id}/next` — next card, already graded-agnostic.
- `POST /api/sessions/{id}/attempts` — `{cardId, answer}` → `{correct, explanation, wrongReason, requeuedAfter, dueAt}`.
- `POST /api/sessions/{id}/finish` — `{exitedEarly}` → `{firstTryCorrect, totalCards, scorePercent, passed, honed, toolGranted}`.
- `GET /api/members/{id}/progress` — per-skill `{completedLessons, totalLessons, percent, lastAnsweredAt}`.

**Dials (from `prd.md > Open Questions`, now fixed):**
- Framing answer correct when **overlap (IoU) between submitted and target rectangle ≥ 0.70**.
  The result screen shows the overlap it measured, so "close enough" is visible.
- Deck **passes at ≥ 80%** of cards correct on first attempt.
- Tool arrives **honed at ≥ 90% first-try correct and ≤ 2 total retries**; otherwise plain.
- Wrong card requeues **after two other cards**; a correct card's next due date is **+3 days**, a
  wrong card **+10 minutes** (so it returns inside the same session).

### Roster & Bags service
Owns people and their bags, and computes the team read.
PRD ref: `prd.md > The bag`, `prd.md > The leader's view`.

- `GET /api/teams/{role}` — team name, roster (members whose primary **or** secondary role matches).
- `GET /api/teams/{role}/bag` — aggregate: per skill `{filledLessons, totalLessons, percent}` and a
  wear state.
- `GET /api/teams/{role}/members` — bag wall: each member's colour, style, worn flag, tool count.
- `GET /api/members/{id}/bag` — earned tools (`skillId, name, honed, acknowledged`), colour, style.
- `PATCH /api/members/{id}/bag` — `{colour, style}`; only self.
- `PATCH /api/members/{id}/preferences` — `{showAnswerOnWrong}`. The reveal toggle belongs to the
  person, not the deck (`prd.md > Product Decisions`), so it is stored on the member.
- `POST /api/members/{id}/bag/acknowledge` — marks the drop as seen so the animation fires once.

Calls Progress for levels and completions; caches nothing. Team fill for a skill is
`sum(completed lessons across role members) / (memberCount × lessonsInSkill)`, which is exactly
`prd.md > The leader's view`'s rule.

### Requirements service
Owns document intake and the parsed requirement set. The only service that calls a model.
PRD ref: `prd.md > Meeting requirements and recommendations`.

- `POST /api/requirements` — multipart: file (`.txt`, `.md`, `.pdf`) **or** `text`. Extracts text,
  fetches the skill catalogue from Content, calls `IChatClient.GetResponseAsync<ParsedRequirements>`,
  validates, stores, returns the draft.
- `GET /api/requirements/{id}` — stored requirement set with provenance.
- `PUT /api/requirements/{id}` — the leader's edits (skills, min levels, headcount, purpose,
  criticality).
- `POST /api/requirements/{id}/confirm` — locks it for recommendation use.
- `GET /api/requirements` — list of saved requirement sets.

Parse contract, as the model sees it: `{ purpose, criticality, skills: [{ skillId, minLevel,
headcount, rationale }], outsideRole: [string] }` where `skillId` must come from the supplied
catalogue. Validation rejects unknown ids, out-of-range levels, and zero headcount; one retry is
attempted with the validation errors attached; then the manual form.

**`outsideRole` exists because of the real brief.** It asks for PA, Streaming, Multimedia, Audio
Recording, Video Recording and Review Video Production — none of which are camera skills, and none of
which this POC has content for. The model must not invent a `pa` skill to be helpful. Those services
come back as plain strings and render under **"Outside this role — not planned here"**, which is both
honest and useful: a leader can see the app didn't silently ignore half their document. Someone who
owns the audio team would get their own plan from the same brief later.

### What the committed fixture parse contains

The `fixture` provider replays this reviewed parse of `samples/cx-rollout-2026-10-17.pdf`. It is the
expected output for slice 5's tests, not just sample data:

| Field | Value |
|---|---|
| purpose | Camera coverage of a group-prayer meeting; podium moved to floor level |
| criticality | high — live, single-room, no re-shoot |
| framing | minLevel 0.5, headcount 2 (crowd camera and floor camera) |
| gear | minLevel 0.5, headcount 1 (floor cam set-up) |
| operation | minLevel 0.5, headcount 2 |
| outsideRole | PA, Streaming, Multimedia, Audio Recording, Video Recording, Review Video Production |

No `switching` requirement comes from the document — which is deliberate. The demo's gap appears when
the leader **adds** live switching by hand at step 13, and then finds nobody on the crew is at level
on it. The gap is the human's judgement, not a model artefact.

### Recommendations service
Owns ranking, strategy and saved plans. Deterministic.
PRD ref: `prd.md > Meeting requirements and recommendations`.

- `POST /api/recommendations` — `{requirementId, strategy}` → `{suggestions, gaps, sustain}`.
- `POST /api/recommendations/check` — `{requirementId, memberIds}` → per-member warnings; used when
  the leader hand-picks.
- `POST /api/plans`, `GET /api/plans/{id}`, `GET /api/plans` — saved plans.

Reads Roster (members, bags) and Progress (levels, recency), Content (skills). Never writes to any
of them.

### Frontend
React SPA. Five route groups matching the nav: Bag, Deck building, Meeting plan, Settings, Profile.
PRD ref: `prd.md > Screens and Layout`.

- **Framing canvas** — pointer events only (`pointerdown/move/up`), rectangle stored in normalised
  0–1 coordinates so it is resolution-independent. Draws the target frame over the submitted one on
  a miss.
- **Card player** — each correct answer nudges an item in the bag and back: a small, 220 ms
  reaction visible at the edge of the screen, so a competency gain is felt before it is counted
  (`prd.md > The member loop`). The gear button opens the reveal toggle without leaving the card.
- **Reveal toggle** — the member's own setting, default on. When off, a wrong answer still says
  "Not quite — try again" and requeues, but the explanation is not shown. Gated in the frontend;
  the data is not a secret, so no extra round-trip.
- **Settings and profile** — the toggle, plus identity: the seeded member you are, and the switch
  between the team bag and your own.
- **Bag** — SVG illustrations driven by tokens; tool fill uses the team percentage, member tools
  are discrete items.
- **Responsive** — one layout, breakpoint at 768px; bottom nav under 768px, side rail above.
- No client-side business rules that the services also own: the frontend displays scores, it does
  not compute pass/fail.

### AppHost
The model for the whole system. References all five services and the frontend, injects service
addresses by name so nothing hardcodes a port, declares the SQLite volumes and the AI provider
settings, and registers a Docker Compose environment so `aspire publish` emits the deployable
artifacts. `AddDockerComposeEnvironment("compose")` plus `WithReference` calls are the single
description of how the system is wired; the compose file is output from it.

## Data Model

Five databases, one per data-owning service. No shared tables, no cross-service joins.

**Content** — `skills(id, role, name, lessonCount)`, `lessons(id, skillId, ordinal, title)`,
`decks(id, roleId, name, skillId)`, `cards(id, deckId, lessonId, type, imageRef, prompt, optionsJson,
targetRectJson, explanation, wrongReasonsJson)`, `assignments(id, deckId, memberId, assignedAt)`.

**Progress** — `attempts(id, memberId, cardId, sessionId, answerJson, correct, firstTry, at)`,
`reviews(memberId, cardId, intervalDays, dueAt, lastResult)`, `sessions(id, memberId, deckId,
startedAt, finishedAt, exitedEarly, firstTryCorrect, totalCards, passed, honed)`,
`lessonCompletions(memberId, lessonId, completedAt)` (unique per pair).

**Roster & Bags** — `members(id, name, primaryRole, secondaryRole, colour, style, worn,
lastActiveAt, showAnswerOnWrong)`, `bagAcknowledgements(memberId, skillId, acknowledgedAt)`. Earned
tools are **not** stored: they are derived from Progress's lesson completions joined to Content's
skills.

**Requirements** — `requirementSets(id, sourceName, sourceText, purpose, criticality, provider,
model, parsedAt, confirmedAt, edited)`, `requirementSkills(id, setId, skillId, minLevel, headcount,
rationale)`.

**Recommendations** — `plans(id, setId, strategy, createdAt)`, `planMembers(planId, memberId,
overridden, warning)`, `planGaps(planId, skillId, shortfall)`.

**State rules, plainly:**
- Answers live in Progress, immediately, per attempt. Nothing is batched.
- A bag's colour and style live in Roster and survive restarts.
- A tool appears because a lesson is complete — computed on read, so it can never drift out of sync.
- A parsed requirement is stored the moment it is returned, before the leader edits it, so the
  original parse and the leader's correction are both recoverable.
- Leaving a deck mid-way keeps the session open and the attempts recorded.

**Seed data (designed to produce the demo numbers and a readable spread):** 6 members — the demo
user plus five teammates — 4 skills × 2 lessons = 8 lessons each, 48 lesson slots across the team.

| Member | Lessons done | Level | Last active | Reads as |
|---|---|---|---|---|
| Elena | 7/8 | high | 40 days ago | the best operator, gone quiet → **worn bag** |
| Marcus | 5/8 | balanced-to-high | 4 days ago | solid, one gap |
| Priya | 4/8 | balanced | 2 days ago | dependable middle |
| Noor | 2/8 | low-to-balanced | 9 days ago | growing |
| Sam | 1/8 | low | 1 day ago | new volunteer, actively learning |
| Danny (you) | 0/8 | empty | — | the demo member |

Per skill across all six: **Framing 6/12, Gear 5/12, Operation 5/12, Switching 3/12 → 19/48 =
39.6%, shown as 40%** — the half-filled bag from `scope.md > What "Working" Looks Like`.

Two deliberate choices in that seed. First, **the worn bag belongs to Elena, the strongest member**,
not the weakest: wear means *quiet*, not *incompetent*, and it gives the sustain recommendation a
real target ("at level on framing and gear, hasn't answered in 40 days"). Putting the shabbiest bag
on the newest volunteer would have the app saying the opposite of what it means. Second, **Switching
tops out at 0.5 across the whole crew**, so a requirement of level 1 on live switching produces a
genuine uncovered gap — which is what the "gaps become work" beat needs to be real rather than
staged.

## File Structure

```
BrainPicker/
├── HoneKit.sln
├── aspire-output/                # generated by `aspire publish`; committed, never hand-edited
│   ├── docker-compose.yaml
│   ├── .env
│   └── *.Dockerfile
├── .env.example                  # AI provider + model + endpoint; copied to .env, which is ignored
├── src/
│   ├── HoneKit.AppHost/          # the model: services, frontend, compose environment, volumes
│   ├── HoneKit.Contracts/        # DTO records shared by every service and the frontend types
│   ├── HoneKit.ServiceDefaults/  # OpenTelemetry, health checks, resilience (Aspire default)
│   ├── services/
│   │   ├── Content/              # decks, cards, skills  → content.db
│   │   ├── Progress/             # sessions, attempts, scheduling → progress.db
│   │   ├── Roster/               # members, bags, team aggregate → roster.db
│   │   ├── Requirements/         # file → text → model → validated set → requirements.db
│   │   └── Recommendations/      # strategies, plans → recommendations.db
│   │       (each service folder: Dockerfile, Program.cs, Endpoints/, Data/, Domain/)
│   └── frontend/                 # React + Vite
│       ├── Dockerfile            # node build → static serve, used by the published image
│       ├── src/routes/           # bag, deck, plan, settings, profile
│       ├── src/components/       # bag art, framing canvas, card types, chips
│       ├── src/api/              # typed clients generated from Contracts
│       ├── src/styles/           # tokens.css, texture, typography
│       └── public/seed/          # sample images used by the seeded deck
├── samples/
│   ├── cx-rollout-2026-10-17.pdf  # the real brief the demo uploads (names + Zoom link redacted)
│   ├── cx-rollout-2026-10-17.md   # same content as text, for the .md path and diffing
│   └── art/                      # stage photographs + bag references (see licensing note)
├── tests/
│   ├── HoneKit.Progress.Tests/    # IoU, scheduling, pass/honed rules
│   ├── HoneKit.Roster.Tests/      # aggregate maths, membership rules
│   ├── HoneKit.Requirements.Tests/# validation, fallback, skill-closure
│   └── HoneKit.Recommendations.Tests/ # strategies, overrides, sustain
├── devpost/                       # planning docs (this file lives here)
└── README.md                      # how to run, in five lines
```

Each service folder holds `Program.cs`, `Endpoints/`, `Data/` (DbContext + seed), and `Domain/`
(the rules, unit-testable without HTTP). The domain folder is what the tests target.

## External Services and Dependencies

| Service | Endpoint | Auth | Cost | Verified |
|---|---|---|---|---|
| OpenAI | `https://api.openai.com/v1/chat/completions` | `OPENAI_API_KEY` bearer | per token, cents per parse | strict `json_schema` supported |
| Ollama (local) | `http://localhost:11434/v1/chat/completions` | key ignored | free | `json_schema` enforced by local server; **Ollama Cloud accepts but does not enforce it** |
| OpenCode Zen | `https://opencode.ai/zen/v1/chat/completions` | Zen API key bearer | pay per use | use the OpenAI-compatible chat models (DeepSeek, GLM, Kimi, Qwen); GPT/Claude/Gemini route via `/responses` and `/messages` |

Exact call shape (identical across all three, which is the point):

```
POST {baseUrl}/chat/completions
Authorization: Bearer {key}
{
  "model": "{model}",
  "messages": [{ "role": "system", "content": "...closed skill list + task..." },
               { "role": "user", "content": "{document text}" }],
  "response_format": { "type": "json_schema", "json_schema": { "name": "ParsedRequirements",
                       "strict": true, "schema": { ... } } },
  "temperature": 0
}
```

`Microsoft.Extensions.AI` wraps this as `chatClient.GetResponseAsync<ParsedRequirements>(...)`; the
OpenAI SDK adapter handles the envelope. Rate limits and exact pricing were **not** verified and
don't matter at this volume; the only real limit is a timeout, which we set at 30 s.

No other external services. No database server, no message broker, no auth provider. Container base
images come from the public registries (`mcr.microsoft.com/dotnet/aspnet:10.0`,
`mcr.microsoft.com/dotnet/sdk:10.0`, `node:*`, `nginx:alpine`) — pin digest or tag at build time so
a rebuild months later doesn't silently change the runtime.

**Data leaving the machine:** only the extracted document text plus the role's skill catalogue, and
only when a live provider is configured. Member names, bags and progress never leave the compose
network. Names in the brief itself are typed text, not structured data — `prd.md > Open Questions`
records that a redaction pass is possible but not built.

## Important Failure Modes

- **No AI key / provider down** → the Requirements screen says reading is unavailable and opens the
  manual requirement form prefilled with the extracted text. The demo continues; nothing is blocked.
- **Model returns an unknown skill or invalid level** → validation fails, one retry with the errors
  attached, then the manual form with the offending items highlighted.
- **PDF yields no text (scanned image)** → "This PDF looks like a scan — paste the text instead,"
  with the textarea already focused.
- **A service is down** → the page that needs it shows a labelled partial state ("Progress is
  unavailable — showing last known levels"), and the rest of the app keeps working.
- **Slow model (over 30 s)** → cancelled with a plain message and the manual form offered; no
  spinner indefinitely.
- **Empty states everywhere they can occur** — empty bag, no saved plans, no requirements yet, and
  a teammate with nothing earned. Each is an invitation, not an error.
- **A service is still starting** — health checks and the frontend's retry mean a cold
  `docker compose up` shows "connecting" for a few seconds instead of a broken page.
- **Volume wiped or first boot** — the service seeds itself and says so in its log; the app opens on
  a fresh demo rather than an empty shell.

## How We'll Know It Works

- **Unit tests** (xUnit) over each service's `Domain/`: IoU grading at the boundary (0.69 wrong,
  0.70 right), scheduling order after a wrong answer, pass/honed thresholds at 79/80/89/90, team
  aggregate with uneven completions, roster inclusion for secondary roles, each strategy's ordering
  on the seeded data, override warnings, and parse validation rejecting unknown skills.
- **Vitest** for the normalised rectangle maths and the queue rendering order.
- **Contract check** — the frontend's typed client is generated from `HoneKit.Contracts`; a
  compile failure is a contract break, which is the cheapest kind to catch.
- **Clean-stack check** — `aspire publish`, then `cd aspire-output && docker compose down -v && docker compose up -d`
  from a fresh clone reaches a working demo with seeded data and no manual step. Run it before
  every recording.
- **Drift check** — `aspire publish && git diff --exit-code aspire-output/` fails when the committed
  compose output no longer matches the AppHost. This is the guard that makes a generated artifact
  safe to commit.
- **Frontend publish check** — the Vite publish path is experimental (`PublishAsStaticWebsite`), so
  slice 1 proves the frontend lands in the generated compose and serves the app. If it doesn't, the
  documented fallback is to eject only the frontend into a four-line hand-written service; the five
  .NET services stay generated either way.
- **Manual demo script** — the 16-step journey in `prd.md`, run end to end before recording,
  including one forced failure (wrong answer) and one forced AI failure (provider set to `broken`).

## Build Slicing for Orchestration Practice

`5-build` will slice along the service boundaries, contracts first, so the learner can drive
separate agents per slice:

1. **Contracts + AppHost + compose environment** — one agent, short. Five empty services that start
   and appear in the dashboard, each with a Dockerfile, plus `AddDockerComposeEnvironment` and a
   first successful `aspire publish`. This lands first deliberately: containerising after the fact
   is a retrofit, and proving the generated compose works while there are only health endpoints to
   check is far cheaper than discovering it with five services of behaviour on top.
2. **Content + Progress** — parallel-safe; Progress depends on Content's card ids only through
   contracts.
3. **Roster & Bags** — depends on Progress read shapes; can start against contracts and seeded fakes.
4. **Frontend, member side** — depends on 2 and 3; the framing canvas can't wait for anything.
5. **Requirements** — depends on Content's skill catalogue; the model adapter and the fixture parser
   are separable, so the fixture can land first.
6. **Recommendations** — depends on Roster + Progress + Requirements contracts; pure logic, well
   testable, and the best slice to parallelise.
7. **Frontend, leader plan side** — depends on 5 and 6.
8. **Polish pass** — art, motion, responsive, empty states, demo script.

Each slice should verify before the next one starts: `dotnet test` green, the affected pages load,
and the dashboard shows the service healthy.

## What Was Simplified and Why

- **SQLite file per service** instead of Postgres/containers — zero infrastructure, and the file
  boundary makes ownership visible. Postgres would add a container and connection management for
  no POC gain.
- **Synchronous HTTP only, no message broker** — the bag is derived on read, so there's nothing to
  publish. An event bus would add a failure mode and a conceptual hop that this product doesn't
  need yet. `scope.md > Future Services` (notifications, chat) is where a broker earns its place.
- **`fixture` provider as the default** — the committed parse of the sample brief means the demo
  never depends on a network call or a key. A live provider is a config switch, and the code path
  is identical, so this is not a fake: it's a recorded input.
- **Seeded members, no auth** — carried forward from `scope.md > Explicitly Cut`.
- **Text extraction for PDF only, no OCR** — scanned briefs fall back to paste. OCR is a whole
  other dependency for a case the demo doesn't need.
- **Recommendations are rules, not a model** — an LLM could write a nicer sentence, but it would
  also be unverifiable and non-repeatable. The explanation comes from the rule.
- **A generated compose file, committed** — one model in the AppHost, YAML as output, so the system
  can't be described two contradictory ways. The accepted cost is that changing the deployment goes
  through the AppHost (and the Aspire CLI on the build machine), and the artifact must be
  regenerated and diffed. Hand-editing is the one thing that would bring the second source of truth
  back, so it's banned in the README rather than left to discipline.
- **SQLite on a volume per service, not a shared database server** — no extra container, and the
  file boundary keeps data ownership visible. The accepted cost is one replica per service.
- **No orchestration** — Compose on one host is the ceiling for now, and that's deliberate.
  Kubernetes would add a control plane, ingress and secrets for five small services on one machine.
  The images are ordinary OCI images, so moving later changes the manifests, not the code.

## Decisions and Open Issues

**Learner's decisions (this stage):**
- Backend in .NET; frontend React + Vite, chosen over Blazor for smooth pointer-driven framing and
  a genuinely separate deployable.
- Topology 1: Aspire-orchestrated services, database per service, synchronous HTTP — extended from
  three services to five when meeting planning was added.
- Meeting planning is **in** the POC, with document upload, AI parsing, editable requirements,
  strategy-based recommendations, and hand-picked overrides.
- Parsing is **its own deployable**, separate from recommendations.
- Provider must be swappable by configuration across OpenAI, Ollama and OpenCode Zen.
- Upload formats: `.txt`, `.md`, `.pdf`.
- **Everything ships as containers, deployed with Docker Compose** — not a laptop-only app. The
  point is portability: any host with Docker can run it, and the recorded demo is the deployed shape
  rather than a dev-server special case.
- **The AppHost is the single source of truth; compose is generated from it.** Chosen after
  researching Microsoft's guidance and how teams actually run Aspire (see below) — the alternative
  was two hand-maintained descriptions of the same system.
- **Art direction:** warm pastels in a hand-painted Ghibli register; the bag is a canvas camping
  pack crossed with a scout kit — patches sewn on the front, tools in and around it. Reference
  photographs supplied by the learner and committed under `samples/art/`.
- **Seeded crew spread:** a definite low, a balanced and a high member, plus one between
  low-and-balanced and one between balanced-and-high.
- **The model gets only what it needs** — extracted text plus the skill catalogue, never the roster
  or member names.

**Implementation details derived from those:**
- Closed skill vocabulary enforced at parse time; schema validation with one retry, then manual
  fallback.
- Deterministic ranking with three named strategies; warnings on overrides rather than blocks.
- Earned tools derived on read plus an acknowledgement flag, instead of a publish/subscribe drop.
- Five SQLite databases on named volumes, one per data-owning service; contracts shared as a
  project; test projects per domain.
- Aspire AppHost as the model, `AddDockerComposeEnvironment` for the target, generated compose
  committed under `aspire-output/` and never hand-edited; a drift check in CI and before recording.
- Service discovery by resource reference in the AppHost, which becomes compose DNS names in the
  published output; health checks gate the frontend; `.env` for provider configuration.
- The worn bag goes to the strongest-but-quiet member, not the weakest, so wear reads as absence
  rather than incompetence.

**One genuine uncertainty, and what resolved it:** when asked to choose a frontend, the learner
asked whether those were the only options and whether other distributed topologies applied, and
asked for a ranked comparison. That was the point of least clarity in the conversation — the
frontend renderer question and the distributed architecture question had been conflated. The ranked
topology list (Aspire + services, BFF, event-driven, modular monolith, Orleans, Dapr, serverless,
Kubernetes, single API) clarified the distinction between *how the UI renders* and *how the system
is partitioned*, and the learner then chose explicitly. **Evidence to check during the build:** that
the same clarity holds in practice — when the fifth service is added, the learner should be able to
say which service owns which data without looking it up. If that isn't true by the end of slice 6,
the boundaries need renaming, not more code.

**Why generated compose — the research behind it (checked 25 Oct 2026):** Microsoft's deployment
guidance states that *"the AppHost is the deployment model, not a Compose workload"* and warns
against hand-editing the generated file as the durable fix. Their documented flow is
`AddDockerComposeEnvironment` → `aspire publish` / `aspire do prepare-compose` / `aspire deploy`.
In practice, three patterns dominate: Aspire for the inner loop with infrastructure-as-code for
production; `aspire publish` into CI with Compose as the deploy contract; and Compose-only for
polyglot stacks. The useful framing from the comparison literature is that the real choice is
"Aspire plus generated Compose, or Compose alone" — and the benefit of Aspire is debuggable host
processes plus injected configuration, not the YAML. Known Aspire gaps that don't bite here:
no `deploy.resources` CPU/memory limits, no custom network isolation, restart policies only at
publish time. Sources: [Aspire Docker Compose deployment](https://aspire.dev/deployment/docker-compose/),
[Docker integration](https://aspire.dev/integrations/compute/docker/),
[publish vs deploy](https://aspire.dev/deployment/deploy-with-aspire/),
[an honest Aspire-vs-Compose comparison](https://startdebugging.net/2026/08/aspire-vs-docker-compose-for-local-multi-service-development/).

**Open issues — carried to the build, none blocking:**
1. **Image licensing.** The stage photographs and bag references came from public search results
   with unverified licences. They stay as local placeholders; before the repo is made public either
   replace them with known-licensed or self-taken photographs, or exclude them and load them
   locally. `6-ship` owns this.
2. **The worn bag on the strongest member.** Elena carries it because wear means quiet, not
   incompetent, and it makes the sustain recommendation real. If that reads wrong on screen, the
   swap is a one-line seed change.
3. **Whether to run a public demo instance.** The compose stack makes it possible; it also has no
   auth and no TLS. That's a `6-ship` decision, not a build one.
4. **The Vite publish path is experimental.** `PublishAsStaticWebsite` carries
   `ASPIREJAVASCRIPT001` in 13.6. Slice 1 proves it works end to end; if it doesn't, eject only the
   frontend into a small hand-written service and leave the five .NET services generated.
