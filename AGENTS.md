# AGENTS.md

## Project

Build With AI (Devpost) learner project. The workflow skills live in `.agents/skills/` and run in order:
`1-start` → `2-scope` → `3-prd` → `4-spec` → `5-build` → `6-ship`.
Do not skip ahead; each one expects the previous step's document to exist.

## UI and design work

When a task involves designing, building, reviewing, or refactoring a user interface, load the relevant design skill in addition to the workflow skill you are already using. They are complementary, not substitutes.

| Target | Skill | Use it for |
| --- | --- | --- |
| Web (HTML/CSS/JS, React, Vue) | `frontend-design` | Aesthetic direction, typography, palette, layout, avoiding templated-looking output |
| React Native / Expo | `vercel-react-native-skills` | Lists, animations, navigation, images, native modules, performance |
| iPhone / iPad / Apple platforms | `ios-design-guidelines` | SwiftUI/UIKit implementation, HIG compliance, Dynamic Type, Dark Mode, accessibility |

Platform-specific implementation guidance from the skill matching the target always wins over generic advice.

## Apple HIG as a conceptual layer

Apply the *intent* of Apple's Human Interface Guidelines to non-Apple UIs too, alongside whichever platform skill applies. Borrow the principles, never the iOS costume.

Portable concepts:

- Clarity, deference, depth: content over chrome, clear hierarchy, progressive disclosure.
- Minimum touch target: 44×44 in Apple points; translate to 44×44 CSS px on web, 48dp on Android.
- Safe areas: respect notches and home indicators (`env(safe-area-inset-*)` with `viewport-fit=cover`).
- Semantic color tokens with real light/dark parity (CSS custom properties, `prefers-color-scheme`) instead of hardcoded hex.
- Dynamic Type as a rem-based type scale that survives 200% browser zoom and user font-size changes.
- VoiceOver labeling as ARIA accessible names; never convey meaning by color alone.
- Motion that respects `prefers-reduced-motion`.
- Immediate feedback (~100ms), optimistic UI, undo for destructive actions, no dead ends.
- Generous hit slop and momentum scrolling.

Do not port these to non-Apple targets:

- SwiftUI syntax, SF Symbols, the squircle app icon, literal 44pt values.
- Bottom tab bars on desktop web.
- Push-navigation stacks and nav-bar-with-back-chevron for multi-column web layouts.
- Swipe-to-delete as the only delete path.
- Haptics, force touch, or iOS-styled sheets and modals.
- Hiding all navigation behind a hamburger because iPhone is single-column.

On web, actual conformance means WCAG 2.2 AA and native web conventions; on Android, Material Design 3. If a design decision conflicts with the target platform's real conventions, follow the platform and cite the HIG principle only as the underlying rationale.
