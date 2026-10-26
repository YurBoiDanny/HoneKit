---
doc: checklist
status: approved
---

# Build Checklist

Build mode: fast (chosen before slice 1; learner checks at the planned checkpoints).

## Slices

- [x] **1. Your empty bag, served from the real stack**
  Becomes usable: `docker compose up` brings up the published stack; `http://localhost:8080` shows the seeded crew roster and your own empty bag, with skills, lessons and members each read from a separate service. The app lands on the member's bag for now; the leader home arrives in slice 6.
  Why now: Bootstrapping lives inside the first usable slice, and it settles the build's two real unknowns while there is almost nothing to break — which Aspire 13.6 project model the installed template emits (`AddProject` vs `AddDotnetProject`), and whether the experimental Vite publish path lands in generated compose. It also puts the kernel on screen first, even empty.
  PRD ref: `prd.md > The Core Journey` (steps 4–5, the empty bag), `prd.md > Screens and Layout` (My bag), `prd.md > States and Boundaries` (first use)
  Spec ref: `spec.md > Stack`, `spec.md > Where It Runs and How Someone Tries It`, `spec.md > Components > AppHost`, `spec.md > Components > Frontend`, `spec.md > Data Model`, `spec.md > File Structure`
  Build: Install the Aspire CLI and templates if missing, start Docker; solution with Contracts, ServiceDefaults and AppHost; Content (skills, lessons, deck summary), Progress (seeded lesson completions) and Roster (six seeded members, bag read from Progress) on one SQLite file each; React + Vite frontend using the design tokens, a first-pass bag SVG, roster and empty-bag view; `AddDockerComposeEnvironment` with a committed `aspire-output/`; `.env.example`; five-line README. If `PublishAsStaticWebsite` fails, eject only the frontend into the small hand-written compose service the spec documents.
  Verify (mechanical): `dotnet build`; `aspire publish`; `git diff --exit-code aspire-output/`; `cd aspire-output && docker compose up -d`; curl each service health endpoint and `http://localhost:8080`; curl the member bag endpoint and confirm `tools: []`; `docker compose down -v`, `up -d` again and confirm the seed returns.
  Learner check: Open `localhost:8080`, look at the crew list and your own bag. Does the empty bag read as an invitation rather than a broken screen, and is this the app you pictured?
  Commit: `Add walking skeleton: empty bag served from generated compose`

- [x] **2. You can answer a card and finish a deck**
  Becomes usable: The Camera Foundations deck opens; image-based fill-in-the-blank cards are answered by tapping a word choice. A wrong answer says "Not quite — try again", shows the explanation and the reason for the picked choice (unless reveal is turned off from the gear button), and comes back two cards later. Finishing shows a score and pass/fail at 80%.
  Why now: First real behavior crossing Content → Progress → frontend. Sessions, grading, requeue and the member's own setting get proven before any visual payoff depends on them, and a deck that passes is what makes a tool earnable in the next slice.
  PRD ref: `prd.md > The member loop` (fill-in-the-blank, right/wrong, reveal toggle, spaced repetition, deck completion), `prd.md > States and Boundaries` (wrong-answer, explanation, deck complete/early exit)
  Spec ref: `spec.md > Components > Progress service`, `spec.md > The Core Journey Through the System` (steps 5–7), `spec.md > Data Model` (Progress; `member.showAnswerOnWrong`)
  Build: Progress sessions/next/attempts/finish plus attempts, reviews and SQLite persistence; choice grading; requeue after two other cards and +10 min / +3 days due dates; pass at 80%; a passing deck completes its cards' lessons idempotently — that is the rule that makes slice 3's tool earnable. Content deck and card reads. Frontend deck list, card player, reveal toggle stored on the member, result screen, and the small bag nudge on a correct answer.
  Verify (mechanical): `dotnet test` on the Progress domain for correct/incorrect grading, requeue position and pass at 79/80; curl a session, post a wrong attempt and assert `requeuedAfter` plus the explanation payload; finish a session and assert `scorePercent`/`passed`; rebuild the stack and repeat the same calls.
  Learner check: Open the deck, deliberately get one wrong, then finish. Does the wrong-answer moment actually teach you the thing, and does the result screen tell you plainly whether you passed?
  Commit: `Answer fill-in-the-blank cards and finish a deck`

- [x] **3. The tool lands in your bag**
  Becomes usable: Passing a deck earns the skill's tool; going home animates it into an otherwise empty bag, and it arrives plain or honed depending on how you ran the deck. The drop fires once, then stays put.
  Why now: This is the kernel's "oh, that's cool" beat, and it lands early enough that feedback can still shape the art, motion and rest of the member loop. It also proves the derived-tool decision — earned tools computed on read, never stored — before more features depend on it.
  PRD ref: `prd.md > The bag` (fills with tooling, plain vs honed, lands on returning home), `prd.md > States and Boundaries` (first use, normal use)
  Spec ref: `spec.md > Components > Roster & Bags service`, `spec.md > Components > Frontend` (bag), `spec.md > Data Model` (bag acknowledgements, state rules), `spec.md > The Core Journey Through the System` (step 8)
  Build: Roster derives earned tools from Progress completions joined to Content skills; honed at ≥90% first-try with ≤2 retries; `acknowledged` flag and the acknowledge endpoint; frontend drop animation on home, honed finish visible, empty bag replaced. Roster domain tests for derivation and the honed boundary.
  Verify (mechanical): curl the bag before and after a passed deck and confirm the tool appears with the right `honed`; restart the stack and re-read; acknowledge and confirm `acknowledged: true`; `dotnet test` for the 89/90 honed boundary and for tools derived rather than stored.
  Learner check: Finish a deck, go home, and watch the tool land. Is that the beat? Tell me what you'd change about where and how it arrives.
  Commit: `Drop earned tools into the bag`

- [x] **4. Framing scenarios**
  Becomes usable: A framing card shows a stage photograph and a draggable, resizable frame. Position the crop for "leader teaching at the lectern" and submit; a miss draws the author's target frame over yours with the explanation, and the card comes back. The seeded deck now mixes both card types.
  Why now: It is the second card type and the only pointer-heavy interaction, so it carries the most UI risk. It arrives after the loop works, so a canvas problem can't hold the whole member journey hostage.
  PRD ref: `prd.md > The member loop` (framing scenario, target frame on a miss), `prd.md > Screens and Layout` (responsive framing canvas)
  Spec ref: `spec.md > Components > Frontend` (framing canvas), `spec.md > Components > Progress service` (the 0.70 dial), `spec.md > Look and Feel > Framing card photography`, `spec.md > How We'll Know It Works` (Vitest)
  Build: Normalised 0–1 rectangle with pointer events only; IoU grading at ≥0.70 in the Progress domain; target overlay on a miss; framing cards seeded with `stage-01.jpg` and `stage-03.jpg` copied from `samples/art/` as local placeholders (licensing is a `6-ship` task). Vitest for rectangle maths and requeue rendering order; xUnit at 0.69/0.70.
  Verify (mechanical): `dotnet test` for IoU at 0.69 and 0.70; `npx vitest run` for normalised coordinates and clamp behaviour; curl an attempt with a rectangle and assert `correct` flips across the threshold; rebuild and load the mixed deck.
  Learner check: Open a framing card on a laptop and on a narrow window. Can you place the frame precisely with a mouse and with a trackpad, and does the miss show you what close enough looked like?
  Commit: `Add framing scenarios with IoU grading`

- [x] **5. Make the bag yours, and look at a teammate's**
  Becomes usable: Pick a bag colour and style (backpack, duffel, sling) and see it stick. Open any teammate's bag read-only and see their colours and kit — including Elena's visibly worn bag.
  Why now: The "show off" half of the kernel, and the first place personalization and the worn-bag seed can be judged on screen. It needs only the member bag that already exists, so it lands before the aggregate read.
  PRD ref: `prd.md > The bag` (personalization, teammates can see it), `prd.md > The leader's view` (bag wall, worn bag seeded)
  Spec ref: `spec.md > Components > Roster & Bags service` (`PATCH /members/{id}/bag`, bag wall endpoint), `spec.md > Data Model` (members), `spec.md > Look and Feel > The bag, visually`
  Build: Colour and style persisted on the member, self only; teammate bag read-only; bag wall listing every role member with their own bag, colours and worn flag; Roster tests for primary-or-secondary membership.
  Verify (mechanical): PATCH a colour and style, GET it back, restart the stack and confirm it survived; GET the bag wall and assert six members with distinct colours and one `worn: true`; `dotnet test` for secondary-role inclusion.
  Learner check: Change your bag, then open a teammate's. Does yours feel like yours, and does Elena's worn bag read as "gone quiet" rather than "bad at the job"?
  Commit: `Personalize bags and open a teammate's`

- [x] **6. The team bag, and the leader's home**
  Becomes usable: The app now lands as the lead on the Camera team bag — one big bag whose four tools fill in proportion to the crew's completed lessons, about half full, with a plainer list on click. The roster sits below it; opening the bag unzips to the bag wall; a nav switch moves between the team bag and your own.
  Why now: The leader side needs the aggregate read, which is real math over the same progress data. Putting it after the member loop means the numbers it shows are numbers the loop already produces, and the landing page finally matches the core journey.
  PRD ref: `prd.md > The leader's view` (team bag, wear, roster, read-only, the switch), `prd.md > The Core Journey` (steps 1–4)
  Spec ref: `spec.md > Components > Roster & Bags service` (aggregate endpoint and fill rule), `spec.md > Data Model` (seed spread and 40% total), `spec.md > The Core Journey Through the System` (steps 1–3)
  Build: Aggregate endpoint computing `sum(completed lessons) / (memberCount × lessonsInSkill)` with a wear state; leader home, roster, unzip to bag wall, nav switch to own bag. xUnit for uneven completions, the 19/48 → 40% seed, and wear.
  Verify (mechanical): `dotnet test` for the aggregate at uneven completions and the seeded total; curl `GET /teams/camera/bag` and compare each skill against the seed table; rebuild, load the leader home, and confirm the four tools read as roughly half filled.
  Learner check: Land on the team bag and read the crew's health without clicking anything. Does one glance tell you where the team is thin, and does switching to your own bag still feel like the same object?
  Commit: `Add the team bag and leader home`

- [x] **7. The brief becomes requirements**
  Becomes usable: A leader uploads `samples/cx-rollout-2026-10-17.pdf` (or pastes text), watches it being read, then edits the parsed requirements — skills, minimum level, headcount — beside the source. PA, streaming and the other non-camera asks appear under "Outside this role — not planned here". With no model available, the same screen falls back to a prefilled manual form.
  Why now: It is the only place a model is called, so it gets its own slice and its own failure path. It also needs the skill catalogue, which slice 1 already serves — one dependency, not two.
  PRD ref: `prd.md > Meeting requirements and recommendations` (upload and parse, closed vocabulary, outside-role services, everything editable, provenance), `prd.md > States and Boundaries` (parse in progress, parse failed)
  Spec ref: `spec.md > Components > Requirements service`, `spec.md > What the committed fixture parse contains`, `spec.md > External Services and Dependencies`, `spec.md > Important Failure Modes`
  Build: Requirements service with multipart upload, PdfPig text extraction, skill catalogue fetched from Content, `IChatClient` over the OpenAI-compatible endpoint with the fixture provider as default, schema validation with one retry, manual fallback, storage before editing, edit/confirm/list, provenance. Frontend Meeting plan screen: upload, reading state, editable items, outside-role list, manual form. xUnit for validation, skill closure, retry and fixture replay.
  Verify (mechanical): `dotnet test` for unknown-skill rejection, level/headcount bounds and the fixture replay; upload the sample PDF with `AI__PROVIDER=fixture` and diff the parse against the spec's table; edit a requirement and confirm; restart and confirm it persisted; set `AI__PROVIDER=broken` and confirm the manual form appears.
  Learner check: Upload the real brief and compare the parse to what you'd have written by hand. Is the draft close enough to edit rather than rewrite, and are the outside-role items obvious without thinking?
  Commit: `Parse a meeting brief into editable requirements`

- [x] **8. The brief becomes a roster**
  Becomes usable: Confirmed requirements become ranked crew suggestions with a reason per person. Switching between critical, balanced and develop changes the order and says what each costs. A hand-picked override is marked as below requirement, never blocked. Gaps offer one-action deck assignment, a quiet-but-strong member is surfaced for a refresher, and the plan saves and reopens with its document.
  Why now: Recommendations need requirements (slice 7) plus the roster and progress reads already built, and this is where the leader loop closes. It is also the most parallel-ready, rule-only piece, so it lands once the contracts it reads have settled.
  PRD ref: `prd.md > Meeting requirements and recommendations` (recommendations, three strategies, hand-picking, gaps become work, sustain, saved plans), `prd.md > Non-Goals` (no ranking of people)
  Spec ref: `spec.md > Components > Recommendations service`, `spec.md > Data Model` (recommendations), `spec.md > How We'll Know It Works` (strategy ordering, override warnings)
  Build: Deterministic ranking with the three named strategies; per-person reasons; gaps and sustain; `POST /recommendations/check` for overrides; plans saved and reopened with the requirement set; gap assignment calling Content's assign endpoint. Frontend strategy switch, override with warning, save/reopen, gap action. xUnit for each strategy on seeded data, overrides and sustain.
  Verify (mechanical): `dotnet test` for strategy ordering on the seeded crew, override warnings and sustain detection; curl each strategy against the confirmed fixture set and confirm differing orders plus the switching gap; save a plan, restart, and reopen it; confirm switching at level 1 has nobody at level.
  Learner check: Ask for a roster, then switch strategies and override one person. Does the app explain the cost without nagging you, and is the gap in live switching obvious enough to act on?
  Commit: `Recommend a roster and save the plan`

- [ ] **9. A leader can author a deck**
  Becomes usable: Create a deck for a role, add both card types — including drawing a framing card's target frame — write the right-answer explanation and a reason per wrong choice, and assign it so it shows up in the crew's deck list.
  Why now: Deliberately last among features. `prd.md > Open Questions` names authoring as the first thing to cut if the build runs long, so it sits where cutting it costs nothing else. Everything it needs — card shapes, target-frame rendering, assignment reads — already exists.
  PRD ref: `prd.md > Deck authoring`, `prd.md > Product Decisions` (authoring stays, trimmed), `prd.md > Open Questions` (cut line)
  Spec ref: `spec.md > Components > Content service` (authoring endpoints), `spec.md > Data Model` (Content), `spec.md > File Structure`
  Build: Content authoring endpoints and validation; frontend deck builder with both card types, target-frame drawing reusing the framing canvas, per-choice wrong reasons, and assignment. xUnit for authoring validation.
  Verify (mechanical): `dotnet test` for card/deck validation; create a deck through the API, add one of each card type, assign it, and confirm it appears in the deck list; rebuild the stack and confirm it persisted.
  Learner check: Build one small deck yourself and assign it. Is the editor small enough to finish a deck without instructions, or is something you'd need every time missing?
  Commit: `Let a leader author and assign a deck`

- [ ] **10. Polish, failure states, and the demo run**
  Becomes usable: The app is the thing the plan describes — layered bag art with tools and sewn patches, the drop and unzip motion, phone-to-desktop responsive layout, labelled partial states when a service is down, empty states everywhere, and the 16-step journey running end to end in the deployed stack.
  Why now: Art, motion and failure behaviour need real content and real states to style, and polishing before them would mean polishing placeholders. It is also the last chance to fix what the demo run exposes before submission.
  PRD ref: `prd.md > Look and Feel`, `prd.md > Screens and Layout` (responsive), `prd.md > States and Boundaries` (all states)
  Spec ref: `spec.md > Look and Feel` (palette, typography, motion, accessibility floor), `spec.md > Look and Feel > The bag, visually`, `spec.md > Important Failure Modes`, `spec.md > How We'll Know It Works`
  Build: SVG bag layers (flap, pockets, tools, patches), 200–300 ms spring motion with a `prefers-reduced-motion` fade, 768px breakpoint with bottom nav under it, empty/partial/offline states, health-gated startup, WCAG 2.2 AA contrast and 44px targets, README, then a full pass of the 16-step demo script.
  Verify (mechanical): `npx vitest run` and `dotnet test` green; `aspire publish && git diff --exit-code aspire-output/`; stop one service and confirm the labelled partial state while other pages work; set `AI__PROVIDER=broken` and confirm the manual fallback; run the 16-step demo end to end from a clean `docker compose down -v`.
  Learner check: Run the demo script yourself at 375px and at desktop width, including one wrong answer and one service stopped. What breaks, what reads wrong, and what would you fix first?
  Commit: `Polish the bag, states, and demo path`

## Hands-on Checkpoints

- [x] Early usable behavior explored — after slice 3, the tool drop: bag art, motion and the rest of the member loop can still be shaped by feedback
- [ ] Final kick-the-tires exploration and feedback completed — after slice 10, the full journey on the deployed stack

## Final Review

- [ ] Final review complete — feedback resolved and learner confirms ready to ship

## Code Tour and App Map

- [ ] Learning activity complete — guided route, focused alternative, prior practice connected, or brief recap
- [ ] Optional edit and transfer reflection addressed — offered/declined/already covered/not applicable as appropriate
- [ ] `devpost/app-map.html` generated from finished code, checked, and shown, including a project-grounded practice to reuse

Activity and evidence:
Route and stops:
Edit outcome:
Reflection:
Activity mode:

## Revisions

- Frontend published as a hand-written nginx image instead of `PublishAsStaticWebsite` — the Aspire static-website publisher reverse-proxies only one backend, and this app has five services the browser must reach. The spec's documented fallback applies: only the frontend is ejected; the five .NET services still come from the generated compose, and the AppHost stays the single model.
- A completed deck grants its tool even when first-try accuracy is below the 80% pass line — otherwise the scripted one-wrong run on a four-card deck (75%) could never produce the tool drop the demo depends on. Pass stays the quality label, honed stays ≥90%, and leaving early grants nothing.
- Seeded a small `Switching Essentials` deck so the switching gap has something to send in one action. The original single framing deck left "gaps become work" with no deck to assign.
