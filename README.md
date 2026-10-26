# HoneKit

What the camera crew learns becomes tools and patches in a bag they carry.

## What is in it

- **Your bag** — an empty bag that fills with tools as you finish decks. Tools arrive plain or
  honed; colour and style are yours. The crew's bags sit side by side on the wall.
- **The team bag** — the leader's home. One bag whose tools fill with the share of each skill the
  whole crew has actually finished.
- **Decks** — image-based fill-in-the-blank and framing scenarios. A framing answer is graded by
  how closely your frame overlaps the author's target.
- **Meeting plan** — upload the brief, correct the parsed requirements, confirm them, then get a
  roster under critical, balanced or develop. Hand-pick with warnings, send a deck to close a gap,
  and save the plan.
- **Deck building** — create a deck, add both card types, assign it.

## Run it

```bash
# the deployed shape: generate compose, build images, start the stack
aspire deploy -o aspire-output
# open http://localhost:8080

# inner loop: the services as local processes, with the Aspire dashboard
cd src/HoneKit.AppHost && dotnet run
```

`aspire-output/docker-compose.yaml` is generated from `src/HoneKit.AppHost`. Change the AppHost and
regenerate it; do not hand-edit the YAML. Images build for the host architecture.

### Reset the demo

The seeded data lives in five named volumes. Removing them and deploying again reseeds everything:

```bash
docker ps -aq --filter 'name=aspire-compose-' | xargs -r docker rm -f
docker volume ls -q | grep -E '_(content|progress|roster|requirements|recommendations)-data$' \
  | xargs -r docker volume rm
aspire deploy -o aspire-output
```

## Reading briefs with a model

`fixture` is the default and needs no key: it replays the reviewed parse of
`samples/cx-rollout-2026-10-17.pdf`, so the demo works offline.

For a live provider, export `AI__PROVIDER` (`openai`, `ollama` or `opencode`), `AI__MODEL` and
`AI__ENDPOINT` before `aspire deploy`. The key is templated into the compose file as
`${AI__APIKEY}`; put it in `aspire-output/.env.Production` (gitignored) or export it when starting
the stack. Never commit a key.

The provider is only used by the Requirements service. Recommendations are deterministic rules, so
they repeat exactly.

## OpenCode Go

OpenCode Go is a subscription with an OpenAI-compatible endpoint for its open models. The app supports
it as a first-class provider:

```bash
AI__PROVIDER=opencode-go
AI__MODEL=glm-5.3-flash        # or kimi-k3, deepseek-v4.1-flash, longcat-2.0, ...
AI__ENDPOINT=https://opencode.ai/inference/go/openai/v1
AI__APIKEY=...                 # OpenCode Go API key, only in .env.Production or the shell
```

Export all three before `aspire deploy`, and keep `AI__APIKEY` in `aspire-output/.env.Production`
(gitignored). Set `AI__ENDPOINT` explicitly: the service ships an OpenAI default, so Go will not pick
up its endpoint on its own.

Only Go's OpenAI-compatible (`/openai/v1`) models work here — the Anthropic and Google-native models
use different endpoints and clients. OpenCode's docs also ask clients to send an `x-opencode-session`
header; this POC does not, which is fine for a demo but worth knowing if Go starts rate-limiting.

The Meeting plan's reader strip shows `opencode-go` and whether the key is present; **Check the reader**
makes one tiny live call and reports the result. It never shows the key.
