---
doc: prd
status: approved
---

# HoneKit — Product Requirements

A flashcard app for the church's production crew, where the skills you prove show up as
tools and patches in a bag you carry — and can show off.
Source: `scope.md > The Unique Kernel`, `scope.md > Who It's For`.
*(Name chosen in `3-prd`; "hone" is the app's verb — you hone, a session is a honing, a tool
arrives honed.)*

*(Amended in `4-spec`, with the learner's approval: leader meeting planning — document upload,
AI-parsed requirements, and recommendations — was brought into the POC. See **Meeting
requirements and recommendations** under Features and Behavior.)*

## The Core Journey

The path end to end, from the demo person's point of view. They are **both** a camera operator
and the **Camera team lead**, so the demo moves between the two sides without losing their bag.
Source: `scope.md > The Core Loop`, `scope.md > What "Working" Looks Like`.

1. **Open the app.** They land as a team lead, on the **team bag** for the team they lead
   (Camera). This is the aggregate that reads the crew's health at a glance.
2. **The header names the team.** Below it, the **roster**: everyone whose *primary or secondary*
   role is Camera. (Our person is on their own list — camera is their primary.)
3. **Open the team bag.** It unzips, and inside is the **bag wall** — each member's own bag,
   side by side. One of them is visibly worn: a teammate who's gone quiet.
4. **Switch to their own bag.** The member view. It starts **empty**.
5. **Open the Camera foundational deck** and answer cards — an **image-based fill-in-the-blank**
   and a **framing scenario**.
6. **Each correct answer**, the bag *reacts* — an item nudges in and back. A wrong answer says
   *"Not quite — try again,"* reveals the right answer by default (and explains it), and that card
   comes back later.
7. **Finish the deck** — every card correct, or they leave early — and see a **score / pass**.
8. **Back home, the tool drops.** The earned tool lands in the bag with an animation, and it
   arrives **plain** or **honed** depending on how well they ran the deck. The bag is no longer
   empty.
9. **Personalize it.** Pick a bag colour and style (backpack, duffel, sling).
10. **Open a teammate's bag** — read-only — and see *their* colours and kit, not the default.
11. **Come back later.** Progress and bag are still there. A bag is never finished.
12. **Plan a meeting, as the lead.** They upload the brief they were sent for the coming service.
    The **Requirements** service reads it and hands back the skills it thinks the meeting needs,
    with a minimum level and a headcount.
13. **Check the parse.** The requirements appear as editable items beside the source document.
    Services the brief asks for that aren't camera work — PA, streaming, audio recording — are
    listed separately rather than invented as skills. They add one the document didn't state:
    live switching, because the room layout demands it.
14. **Ask for a roster.** The **Recommendations** service ranks the crew against the confirmed
    requirements and explains each suggestion. They switch strategy: *critical* for a service
    that cannot miss, *balanced* for one where a junior should be paired with a senior.
15. **Override it.** They hand-pick someone the ranking didn't lead with, and the app tells them
    what that choice costs — without blocking it.
16. **Save the plan.** The gaps become assignable work: the people below level on switching can be
    sent the deck in one action.

**Success for the demo** is that both sides land: on the member side, an empty bag visibly fills
with earned gear; on the leader side, one big bag reads the whole crew's health at a glance; and
a document that used to be read and forgotten becomes a roster and a training plan.

## Screens and Layout

Surfaces, and how someone moves between them.

- **Team bag — leader home.** The default landing for a lead. Top: the team name. Below: the
  roster of members (name, their bag, their standing). Center: the big aggregate **team bag**,
  which *is* the health read. *(No separate dashboard strip and no separate health bar — both
  cut; the team bag says it visually.)*
- **Bag wall.** Reached by opening the team bag — it "unzips" to reveal members' bags side by
  side. Read-only.
- **My bag — member view.** Your own bag: empty → filled, and personalizable.
- **Teammate's bag.** A read-only look at another member's bag, showing their personalization.
- **Deck view / card player.** Where cards are answered, one at a time.
- **Deck result.** The score / pass screen at the end of a deck.
- **Deck authoring.** Create a deck, add cards of the two POC types, assign it.
- **Meeting plan — leader.** Upload a requirements document, review and edit the parsed
  requirements, choose a selection strategy, see ranked crew with reasons, override by hand, and
  save the plan. This is where a document becomes a roster.
- **Settings and profile.** Minimal — but home to the **"show the answer when I get it wrong"**
  toggle. The same toggle is reachable from a **gear button on the card screen**.
- **Responsive by design.** One interface that adapts from a **phone** to a **desktop**: the bottom
  navigation becomes the desktop navigation, and the framing canvas stays usable at both sizes.

**Bottom navigation:** Bag view · Deck building · Meeting plan · Settings · Profile.

**This person's duality** is a switch between the **team bag** and **their own bag** — the same
nav, two states of the same object.

## Look and Feel

Source: `scope.md > Inspiration & Identity`.

- **Bright, outgoing, warm, hand-painted — Ghibli.** Deliberately **not childish**: this is the
  crew's tool, not a school quiz.
- **Tactile and earned.** Scouting and military patches, badges, ranks; a backpack or toolbag
  with gear poking out the top and patches sewn on the front. Competency is something you can
  *see* and *wear*.
- **The bag carries the health story.** A well-kept crew shows a shiny, packed bag with clean,
  rust-free tools; a quiet crew shows one worn and poorly kept. Health is never a chart.
- **"Honed" is product vocabulary.** A tool arrives plain or with a *honed* finish — the word
  does the work of "how well did you run it."

## Features and Behavior

### The member loop

Two card types in the POC. Source: `scope.md > The POC Boundary`.

- **Image-based fill-in-the-blank.** An image, a sentence with a missing word (jargon or gear),
  and a short set of **word choices** you tap to fill the blank. **No free text and no fuzzy
  matching** — the author supplies the choices, so there is always exactly one right answer.
- **Framing scenario.** A photo with a **draggable, resizable frame**. You position the crop for
  the situation ("leader teaching at the lectern") and submit. Correctness is judged by how close
  your frame sits to the **author's target frame**, in **both position and size**; inside a
  tolerance, it counts as correct. *(The exact tolerance is a build-time dial for `4-spec`; the
  product rule is "close enough is right.")* On a miss, the **target frame is drawn over yours**
  with its explanation, so they can see what close enough looked like.
- **Right answer:** the bag reacts — an item nudges in and back — as visible proof of a real
  competency gain.
- **Wrong answer:** *"Not quite — try again."* The correct answer is then **revealed** so they can
  move on. Reveal is **on by default**, and the person can turn it off with the **gear button** on
  the card screen — a setting that belongs to *them*, not to the deck.
- **Every card explains itself.** When the answer is revealed, the correct answer carries a short
  **explanation**, and the choice the person picked carries a short **reason it's wrong**, shown
  directly beneath that choice. The deck author writes both.
- **Spaced repetition:** a card answered wrong returns **sooner**; a card answered right drifts
  **further out**. The learner keeps getting another chance.
- **Deck completion:** a deck ends when **every card is correct**, or when the person **exits
  early**. Either way it closes on a **score / pass** result.

### The bag

Source: `scope.md > The Unique Kernel`, `scope.md > The POC Boundary`.

- Starts **empty** on first use.
- **What "at level" means.** A member is **at level** on a skill once they've completed that
  skill's lessons. Their own bag reflects the **percentage of their role's lessons they've
  completed**.
- **Fills with tooling.** For the POC, **completing a deck drops one tool** into the bag. It lands
  with an animation when the person **returns to the home screen** after finishing.
- **Performance shows in the finish.** How often and how accurately they answered decides whether
  the tool arrives **plain** or **honed** (shinier, better).
- **Personalization:** **colour** and **bag style** (e.g., backpack, duffel, sling). Rewards,
  special designs, and richer customization are `Later`.
- **Teammates can see it.** Opening someone's bag shows *their* colour and style — this is the
  "show off" half of the kernel.

### The leader's view

Source: `scope.md > The Core Loop`, `scope.md > What "Working" Looks Like`, `scope.md > The POC Boundary`.

- **Team bag — the health read.** One aggregate bag for the team. Each tool's fill is **the share
  of that skill's lessons completed across every member of the role** — total completions divided
  by (members × lessons) — so ten camera crew with four fully finished reads 40%. The filled part
  is solid and in colour, the rest an **empty outline**. In the demo, the Camera bag sits about
  half filled.
- **Wear tells the story.** An active crew's bag looks kept and shiny; a quiet crew's bag looks
  worn and dull. The POC **seeds** a worn bag for an inactive member rather than simulating decay
  over time (live decay is `Later`).
- **Bag wall.** Opening the team bag reveals the members' own bags side by side.
- **Roster.** The team's members are everyone whose primary **or** secondary role is that team's
  role — so the Camera lead sees camera-primary *and* camera-secondary crew.
- **Read-only.** Leader views report; they never change anyone's bag.
- **The switch.** The lead toggles between the **team bag** and **their own member bag**.

### Deck authoring

Kept in the POC by the learner's decision (see **Product Decisions**). Scoped tight to the two
POC card types.

- **Create a deck**, give it a name and the role it belongs to.
- **Add cards** of the two types: a fill-in-the-blank (image, sentence, choices) and a framing
  scenario (photo + the **target frame** the answer is graded against). Each card also carries an
  **explanation for the right answer** and a **short reason for each wrong choice**.
- **Assign** the deck to the team (or members).
- This is deliberately the smallest editor that proves *a leader can make and push content* — not
  a general-purpose authoring suite.

### Meeting requirements and recommendations

Added in `4-spec` by the learner's decision. Source: `scope.md > Future Services` (rostering,
scheduling and inventory), brought forward into the POC.

The leader already receives a requirements document for every meeting they serve. This feature
turns that document into a roster and a training plan.

- **Upload and parse.** The leader uploads the brief (`.txt`, `.md`, `.pdf`). The **Requirements**
  service extracts the text and a model returns a structured requirement set: **skills, minimum
  level, headcount**, a one-line purpose, and a criticality guess.
- **Closed vocabulary.** The model may only name skills the app already knows — the role's skill
  catalogue comes from the Content service and is supplied to the model. An invented skill is a
  parse failure, not a new skill.
- **Outside-role services are named, not swallowed.** A real brief asks for PA, streaming, audio
  recording and multimedia alongside camera work. Those come back as a separate list — "outside
  this role, not planned here" — so a leader can see the app understood the whole document, and
  knows exactly what it isn't covering.
- **Everything is editable.** The parsed requirements are shown as editable items, not as a wall
  of text: remove a skill, change a minimum, change headcount, add one the document never stated.
  The model's output is a first draft; the leader owns the requirement.
- **Provenance.** The screen says which document the requirements came from and when they were
  read, so a stale or wrong parse is visible rather than silent.
- **Recommendations.** Against the confirmed requirements, the service ranks the crew and
  explains each suggestion in one sentence — coverage of the required skills, current level, and
  how recently they last answered.
- **Strategies.** Three named objectives over the same data:
  - **Critical** — maximise coverage of the required skills. The meeting cannot afford a miss.
  - **Balanced** — pair the strongest member on a required skill with someone below level on it.
  - **Develop** — deliberately prefer members below level, treating the meeting as training, and
    show the lower readiness score that results.
- **Hand-picking is allowed and informed.** The leader can override any suggestion. The app shows
  the consequence ("below this meeting's requirement") without blocking the choice.
- **Gaps become work.** Skills the crew can't cover produce a suggested assignment: send the
  relevant deck to the people below level, in one action.
- **Sustain, not only improve.** A member at level who has gone quiet is surfaced for a refresher
  rather than being invisible.
- **No ranking of people.** The crew is never ordered by percentage in a way that reads as a
  leaderboard. Suggestions are ordered by fit to *this meeting*, and the reasons are always shown.
  Source: `prd.md > Non-Goals`.
- **Saved plans.** A confirmed plan persists and can be reopened; the requirements document
  itself is kept with it.

## States and Boundaries

- **First use** — the member's bag is empty; the app makes the emptiness inviting rather than
  broken.
- **Normal use** — cards answer, the bag reacts, decks complete.
- **Empty state** — a teammate with an empty bag shows an empty bag (not an error).
- **Wrong-answer state** — *"Not quite — try again,"* answer revealed, card queued to return.
- **Explanation state** — the revealed right answer is explained, and the wrong choice the person
  picked says why it's wrong. If they've turned reveal off in settings, this is skipped.
- **Deck complete / early exit** — both reach the score / pass result.
- **Persistence** — progress and bag survive closing the app. You come back to where you were.
- **Read-only** — leader views and a teammate's bag are viewable but not editable.
- **Worn-bag state** — an inactive member's bag renders worn and dull (seeded in the POC).
- **No permissions or auth** — seeded members stand in for accounts.
- **Parse in progress** — the uploaded document shows a reading state; the leader can cancel and
  type requirements instead.
- **Parse failed or unavailable** — no model key, a slow model, or an invalid parse falls back to
  the manual requirements form, with a plain explanation. The demo never depends on the model.
- **Requirements unconfirmed** — recommendations cannot run until the leader confirms the
  requirement set; the screen says so rather than showing a guess.
- **Nobody covers a skill** — the roster shows the gap plainly and offers the assignment instead
  of presenting a false fit.
- **Override warning** — a hand-picked member below requirement is marked, and the plan can still
  be saved.

## Product Decisions

- **Name: HoneKit.** The "hone" verb gives the app its vocabulary; the kit is the bag of tools.
- **Leader view first.** The demo person is greeted as a team lead, because they are one. A member
  with no lead role would land on their own bag. *(Assumption — verify during the build.)*
- **Deck authoring stays in the POC**, trimmed to the two card types — chosen against `scope.md >
  Later`, because a content service is a legitimate seam for the microservices goal. Flagged as the
  piece most likely to eat the build window.
- **Dashboard strip cut; separate health bar dropped.** The team bag *is* the health read —
  a second indicator would say the same thing twice.
- **Extra card types deferred.** Multiple choice, reorder/drag, and the rest stay in `scope.md >
  Later`. The fill-in-the-blank takes **choices**, not free text, to remove grading ambiguity.
- **Spaced repetition kept** — wrong cards sooner, right cards later.
- **Performance-based finish kept** — tools arrive plain or honed.
- **Personalization kept simple** — colour and bag style only.
- **Wrong-answer reveal defaults on, and is the person's own setting** — reachable from the gear
  button on the card screen. The deck doesn't decide it.
- **Cards explain themselves** — the author writes an explanation for the right answer and a reason
  for each wrong choice.
- **"At level" is lesson completion** — individual progress is the percentage of the role's lessons
  completed; the team bag scales that percentage across everyone in the role.
- **Responsive from phone to desktop**, not a phone-only layout.
- **Worn bags are seeded, not simulated** — the contrast must show in one demo glance.
- **Meeting planning came into the POC in `4-spec`** — the leader's brief becomes requirements,
  requirements become a roster, and gaps become assignments. This pulls `scope.md > Future
  Services` (rostering) forward. Chosen because it closes the loop the leader side otherwise
  leaves open, and because it gives the fourth and fifth service boundaries a real reason to exist.
- **The model parses; it does not decide.** AI turns a document into a draft requirement set.
  Ranking, strategy, and warnings are deterministic rules over progress data, so every
  recommendation can explain itself and the demo repeats exactly.
- **The model may only use known skills.** The role's skill catalogue is passed in with the
  request; anything outside it fails validation and falls back to manual entry.
- **Selection strategy is the leader's explicit choice**, not a hidden weighting — critical,
  balanced, or develop. The app shows what each strategy optimises for and what it costs.
- **Hand-picking beats ranking, with a warning.** The leader can always override; the app states
  the consequence and saves anyway.
- **Parsing is its own service** — a separate deployable from recommendations, so the model
  dependency and its failure modes stay isolated from the rules engine.
- **Provider is configuration, not code** — OpenAI, Ollama, or OpenCode Zen behind one interface.

## What We're Building

Everything the proof of concept must do to be complete.

- One role with content: **Camera**.
- **The member loop:** own bag from empty; the two card types (fill-in-the-blank with choices;
  framing scenario graded by frame closeness, with the target frame shown on a miss); right/wrong
  feedback with a per-card **explanation** of the right answer and of the picked wrong choice; the
  **reveal toggle** in settings; spaced repetition; deck completion on all-correct or early exit;
  a score / pass result.
- **The bag:** fills as decks complete; the tool lands on returning home; plain vs honed finish;
  colour and style personalization; a teammate's bag viewable, read-only.
- **The leader side:** team bag as the aggregate health read, each tool filled by the share of that
  skill's lessons completed across the whole role (half-filled in the demo, with wear showing an
  inactive member); the bag wall; the roster (primary or secondary membership); the switch between
  team bag and own bag.
- **Deck authoring:** create a deck, add the two card types (including a framing target frame and
  the per-card explanations), assign it.
- **Seeded members** with varied progress, including one inactive member with a worn bag.
- **Persistence** of progress and bag between sessions.
- **A responsive interface** that works on a phone and a desktop.
- **Two or three real service boundaries** — enough for the orchestration and microservices goals
  to have a genuine seam, chosen in `4-spec`. *(Revised in `4-spec`: five services — Content,
  Progress, Roster & Bags, Requirements, Recommendations — with the last two added for meeting
  planning.)*
- **Meeting planning:** upload a requirements document, parse it with a model into editable
  requirements, run recommendations under three strategies, hand-pick and override with warnings,
  surface gaps and sustain needs, and save the plan.

## Deferred From the POC

Products the app implies but is not building now, with the reason.

- **The dashboard strip** (participation trends, dropping competency, comments to address, deck
  quality). This is the leader **analytics** service from `scope.md > Future Services`; it says
  nothing the team bag doesn't already say in one glance.
- **A separate health bar** — redundant with the team bag.
- **Multiple choice, reorder/checklist, audio melody ID, terminal scenarios** — `scope.md > Later`
  card types.
- **Content for video directing, multimedia, audio, networking** — one role only for the POC.
- **Real accounts and registration** — seeded members prove the same thing without the timesink.
- **Live bag wear-and-tear over time** — the POC seeds a worn bag instead.
- **Richer bag customization, rewards from leaders, badge design tooling, the leader bag,
  multiple/role-shaded bags** — `scope.md > Later`.
- **Comments on decks** — not in scope; surfaced while discussing the dashboard.
- **Discord / WhatsApp notifications** — `scope.md > Later`; a seam may be left, not built.
- **A service per feature** — five services are justified: Content, Progress and Roster & Bags
  each own data nobody else owns, and Requirements and Recommendations each isolate a distinct
  dependency (a model; a rules engine). Nothing else is split for its own sake. *(Revised in
  `4-spec` from "two or three" when meeting planning was brought in.)*

## Possible Later Enhancements

- Content for the other roles and more card types, once the loop is proven.
- Deck authoring growing into a real authoring suite with assignment targeting.
- The analytics service feeding rostering, scheduling, and inventory.
- Team-leader recognition, where leaders hand out special designs as rewards.
- Notification nudges ("something new is up — go look").

## Non-Goals

Specific things this project will not do now, and why.

- **It will not score or rank people against each other.** Comparison exists to encourage and to
  offer peers as proof it's possible — never to demoralize. Source: `scope.md > Why This Matters
  to the Learner`.
- **It will not be a general-purpose study app.** The kernel is the bag; remove it and this is a
  generic quiz app.
- **It will not build the future services** (analytics, chat, notifications) now — they are named
  so the boundaries leave room, not so they ship this weekend.
- **It will not be a scheduling or rota system.** A saved plan is advice for one meeting, not a
  calendar, a shift assignment, or something the crew can accept or decline. No availability
  data, no dates driving logic, no notifications. Those stay in `scope.md > Later`.

## Open Questions

- **Exact tolerance for a "correct" framing.** Build-time dial for `4-spec`. *(Resolved in
  `4-spec`: overlap of submitted and target frames, ≥ 0.70 counts as correct.)*
- **Deck pass threshold and score formula.** Build-time dial for `4-spec`. *(Resolved in
  `4-spec`: pass at ≥ 80% of cards correct on first attempt; honed at ≥ 90% with ≤ 2 retries.)*
- **Which two or three service boundaries.** Decided in `4-spec`. *(Resolved: five services, as
  listed in `What We're Building`.)*
- **Whether deck authoring fits the window at build time.** If the build runs long, authoring is
  the first thing to cut — its surface is the largest and its link to the kernel the weakest.
- **Where the card imagery and bag art come from.** Not yet decided — the framing scenario needs
  photographs, and the bags need illustrations in the hand-painted direction. Options are
  bundled stock photography, drawn SVG art, or the learner supplying images. *(Carried into
  `4-spec` as a gap to resolve before build.)*
- **How much of the requirements document reaches the model provider.** Real briefs may name
  volunteers. Nothing has been decided about redaction, and it matters more for a consumer
  provider than for a local model.

## Revision after the first build (26 Oct 2026)

Agreed in the final-review pass, within the same proof of concept.

- **Skills are Framing, Setup, Tracking, and Presets & Traces.** The crew's work is expressed in
  those four, and the seeded spread keeps the same shape (Elena 7/8, one worn bag, 40% team).
- **Two new card types.**
  - **Ordering** — the learner reorders the steps of a real procedure from the crew's camera
    checklist. Correct order is the answer; a miss shows which step is out of place.
  - **PTZ board** — an interactive Bolin-style controller: choose a camera, recall a preset
    (PRESET/CALL plus a number), play a trace (1–4). The viewfinder moves when the camera is sent
    somewhere. Used in decks and in authoring.
- **The bag hub.** Clicking a bag plays the unzip, darkens the room, pushes in, and opens a
  full-screen hub with the bag at the centre and options around it: tools, the crew, personalization,
  and the numbers (team bag). Leave with Esc, the close control, or by choosing a destination.
- **Deck authoring** gains both new card types, including placing the target frame for a framing card.
- **The reader's status** is shown on the Meeting plan: provider, model, key present, and whether it
  is the offline fixture or a live endpoint.

The non-goals are unchanged: no ranking of people, no scheduling system, no auth.
