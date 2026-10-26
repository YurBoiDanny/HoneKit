---
doc: scope
status: approved
---

# BrainPicker (working name — from your project folder, change it freely)

A flashcard app for the church's production teams, where what you learn shows up as tools and
patches in a bag you own — and can show off.

## The Unique Kernel

The bag. It starts empty. Every card you complete puts something visible into it — a camera, a
mixer, a laptop, a patch sewn on the side. Competency becomes something you can *see*, and
because you can share it, comparison stops being a scoreboard and becomes "how'd you get that?"
— a doorway to mentorship, conversation, and celebrating each other. Remove the bag and this is
a generic quiz app with a leaderboard.

## Who It's For

A volunteer on the church's production crew. Concretely, a camera operator — you, essentially.
They serve Sunday services, but also midweek meetings, Saturdays, and the occasional special or
extra event. They need to hold proficiency in a primary *and* a secondary role so a leader can
call on them flexibly, and today have no way to see their own growth or learn from peers except
by showing up.

Second: a team leader. They serve on the team like everyone else, but they also grow people and
need a read on where the team actually is.

You are **both** — a camera operator *and* a team lead — so the proof of concept demos both sides,
not just the member's.

## The Core Loop

Open the app → your primary/secondary role focus is right there → answer cards → the bag fills,
visibly → personalize it, share it, peek at a teammate's. You come back because a bag is never
finished.

For a leader: open the **bag wall** — every member's bag side by side — or step back to the
**team bag**, one big bag per role where each tool fills in proportion to how much of the team
is genuinely at level. Their choice; the leader switches between the two.

## Inspiration & Identity

- Scouting and military: patches, badges, ranks — earned through demonstrated ability, worn
  where people can see them.
- A backpack or toolbag: gear poking out the top, patches sewn on the front, personal.
- It should feel like *the crew's tool*, not a school quiz. Tactile, earned, celebratory.
- Discord's "something new is up, go look" nudge (deferred — see Later).

## Why This Matters to the Learner

The motivation is communal, not competitive. Comparison should help someone see where they can
improve and show them peers who prove it's possible — never produce negative emotion. The
personal stake is direct: camera is your primary, video directing your secondary, and you lead a
team too — so you sit on both sides of this. It's your own problem, a portfolio piece, and a
chance to practice what you've been meaning to get better at.

Readiness matters constantly, not just on a Sunday. The crew is called on midweek, on Saturdays,
and for special or extra events, often with fewer people than the regular service. Competency has
to be there *when the event appears*, so the app needs to make each person's readiness visible and
achievable *between* events.

## What "Working" Looks Like

The demo shows both sides.

**As a member:** they open the app (seeded profile), see an **empty bag**, get a camera
foundational deck, answer an image-based fill-in-the-blank on jargon and a framing scenario —
positioning the crop for "leader teaching at the lectern" — and the bag visibly gains its first
tools and patches. They open a teammate's bag and see theirs.

**As a team lead:** they open the **bag wall** — the crew's bags side by side — and can step back to
the **team bag**: one big bag for the whole camera team, where the camera is only half filled
(solid, in colour) and the other half sits as an empty outline, because only part of the crew is
actually proficient yet.

One person is both, so the demo moves between the two without losing their own bag.

The "oh, that's cool" beat: the empty bag filling — a tool landing in it the moment you get an
answer right.

## Learning Goals This Must Serve

Two, paired, and in real tension with scope:

- Practice **multi-agent orchestration** — you drive the coordination, not the agent.
- Practice **distributed / microservice architecture**, small-scale.

Both want *more* surface, not less. So the PoC is cut to keep **real seams** rather than removed
surface. `4-spec` picks the boundaries.

## The POC Boundary

In:

- One role: **Camera**. Video directing is a listed team/role, but with no content yet.
- **Both sides demoed:** a team member's loop, and a team lead's two leader views — the same person
  is both.
- Two card types: **image-based fill-in-the-blank** (jargon, gear) and a **framing scenario**
  (position the shot for a given situation).
- The **bag**: empty → fills with tools/patches → personalization ("bag flavor" — color, bag style,
  glossy or matte patches and pins, a drawing) → view a teammate's bag.
- **Leader view, two modes the leader can switch between:** the **bag wall** — members' own bags
  side by side — and the **team bag**, one big aggregate bag per role where a tool fills in
  proportion to how much of the team is at level (the filled part solid and in colour, the rest
  an empty outline), with a plainer list view on click. Read-only.
- **Two or three real service boundaries**, so the orchestration and microservice goals have a
  genuine seam to work with (architecture decided in `4-spec`).
- **Seeded members** instead of real accounts.

## Later

Product features you want, sequenced after the PoC — with an integration point left where one is
cheap to leave, so they attach later without a rewrite:

- Content for video directing, multimedia, audio, networking
- More card types: ordering/checklist, audio melody ID, terminal scenarios
- Deck authoring and targeted assignment ("push this to the people who need it")
- Badge design tooling; the leader bag; multiple / role-shaded bags
- Richer bag customization (custom drawings, more styles and finishes)
- Discord notifications (a seam in the PoC), then WhatsApp
- Real accounts and registration

## Future Services (planned for, not built)

You'd rather these be their own services than bolt-ons, because they tie back into training.
Named here so the PoC's boundaries leave room for them:

- **Leader analytics → rostering, scheduling, and inventory**: who's best for the job, what each
  team needs, training plans, who to schedule. Feeds off the same progress data the bag uses.
- **Chat**
- **Notifications** (Discord / WhatsApp)

## Explicitly Cut

Nothing is cut from the *product vision* — the above is sequencing. Cut from the *PoC*, with
reasons:

- **Real authentication and registration.** Seeded members prove the same thing in the demo;
  auth is a timesink that says nothing about the kernel.
- **Building any future service now.** They're named so we plan for them, not so we build them
  this weekend.
- **A service per feature.** Two or three seams chosen so the distribution is *justified* — not
  a dozen services holding nothing.

## Revision after the first build (26 Oct 2026)

The learner's final-review pass reshaped the content and the bag interaction, within the same POC:

- **Four skills, renamed:** Framing, Setup, Tracking, and Presets & Traces replace Framing, Gear,
  Operation and Switching. The seed spread, requirements, recommendations and decks move with them.
- **Two more card types:** an **ordering** card built from the crew's real camera checklist, and a
  **Bolin PTZ board** for recalling presets and playing traces. These join the image fill-in-the-blank
  and framing scenario.
- **The bag is an object you open:** clicking it unzips, the room darkens, the camera pushes in, and
  a full-screen hub puts the bag in the centre with angular options around it.
- **The brief reader is visible:** the Meeting plan names the provider and whether it is ready.

Everything else in **The POC Boundary** holds.
