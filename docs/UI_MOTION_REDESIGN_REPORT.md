# LinguaFlow STEP 14.2 — UI, motion and visual QA report

Date: 2026-11-10. Scope: the existing standalone Blazor WebAssembly app. No course source, LCF schema, IndexedDB record, grading rule, import/export contract or route was changed. No commit or push was made in this step.

## Phase checklist

| Phase | Implemented and verified |
| --- | --- |
| A — inventory/baseline | 23 Razor pages, shared components, CSS, MudBlazor theme, JS, route list, baseline screenshots; Release build and 84 tests passed |
| B — foundation | New locally bundled Be Vietnam Pro font, shared light/dark tokens, wider fluid shell, revised sidebar/header/card/button/form rules and MudBlazor palette; build and 84 tests passed |
| C — critical defects | Lesson main column widened, Unit assessment collapsed and grouped by skill, raw skill keys localized, visible Markdown emphasis markers removed from assessment requirements, mobile select width corrected, Unit breadcrumb fixed; build and 84 tests passed |
| D/E — learning and other pages | Shared responsive design applied to catalog, dashboard, roadmap, reader, exercises, vocabulary, flashcards, practice, progress, backups, materials, help, About, privacy and settings; build and 84 tests passed |
| F — motion | Small local Web Animations module plus CSS feedback/flashcard transitions; reduced-motion and no-JS fallback; build and 85 tests passed |
| G — visual QA | Chrome checks at 360, 390, 768, 1024, 1366, 1692 and 1920px in light/vi-VN and dark/en-US. 21 routes were checked at 390 and 1692px, home/Unit at all widths; no horizontal overflow or Blazor error reported. Captured before/after images. |
| H — regression/docs | 85 .NET tests passed (52 Core, 21 Web, 12 Converter). Release publish validator and headless Chrome smoke passed under `/LinguaFlow/`, including offline exercises and manual ChatGPT package/result workflow. |

## Actual page inventory and redesign status

“Shared design” means the page uses the new global tokens, typography, form, card, action and responsive rules without changing its learning logic. “Route check” means Chrome loaded it at 390 and 1692px and verified no horizontal overflow or visible Blazor error; it is not a full manual visual approval.

| Page | Redesign status | Browser route check |
| --- | --- | --- |
| Home | Hero, onboarding, language cards, fluid width | Yes |
| Language / course catalog | Course tiles and responsive grid | Yes (`/languages/en`) |
| Course landing/dashboard | Metrics, action groups, Unit cards | Yes |
| Course roadmap | Milestone cards and responsive grid | Yes |
| Unit reader | Wider reading column, compact assessment, section navigation, button spacing | Yes; screenshots |
| Vocabulary library | Search/filter controls and word grid | Yes |
| Flashcards | Larger card, reveal motion, controls | Yes |
| Review dashboard | Metrics and responsive cards | Yes |
| Practice Center | Wizard spacing, form fields and actions | Yes |
| Practice import | Wizard, preview and result criteria | Yes |
| Practice result detail | Shared card/typography system | Not separately checked with a saved result in visual audit; production smoke visits it |
| Course progress | Metric and chart spacing | Yes |
| Course backup | Export/import cards and preview layout | Yes |
| Course materials | Resource cards and grid | Yes |
| Course offline | Shared cards, actions and status | Yes |
| Learning goals | Shared form/card styling | Yes |
| Personal workspace | Notes/bookmarks cards and filters | Yes |
| Mistake notebook | Shared review/form styling | Yes |
| Settings | Selects and preference rows | Yes |
| Help | Search, categories, article layout | Yes |
| About | Hero, factual author card, capability grid | Yes |
| Privacy | Readable cards and grid | Yes |
| Not found | Shared state design | Not separately checked |

`/languages/ja` and `/languages/zh` use the same Language page as English and remain coming soon. They were not separately captured in the visual run. The global shell, footer, breadcrumbs, loading/empty/error states, dialogs and snackbars use the same shared tokens; the mobile drawer has keyboard focus management.

## Root causes and fixes

- The global `.content-wrap` capped the entire app at 1180px and added large gutters inside a 252px sidebar layout. The lesson then added its own 235px section sidebar, leaving only about 837px for actual content at 1692px. The new shell allows up to 1640px, producing a 1430px content region and about 1082px lesson column at that viewport.
- `UnitAssessmentSummary` rendered every rule as a dense row before the lesson, showed raw `grammar`/`vocabulary` identifiers and printed `**` from source Markdown. It is now a compact, closed-by-default detail panel; repeated skill rules are grouped without deleting them, labels use `vi-VN`/`en-US` resources, and authored rule text is safely displayed as encoded plain text with emphasis markers removed. No course HTML is executed.
- Mobile header elements shrank the language `<select>` until Vietnamese text was clipped. The status hint becomes a compact dot while preserving its accessible status text, and the select receives enough fixed width. The screenshot now shows “Tiếng Việt” completely at 390px. Locally bundled Be Vietnam Pro includes Vietnamese glyphs and avoids CDN/offline font failure.
- Lesson section navigation previously had a height cap and its own scroll pane. It now uses normal page flow on desktop and a collapsible picker on mobile. This removes the tiny nested vertical scrollbar.

## Design system and motion

Tokens live in `wwwroot/css/redesign.css`, loaded after the legacy stylesheet during controlled migration. They define indigo/sky/mint/amber/coral accents, neutral surfaces, light/dark text and borders, spacing from 4 to 48px, radii, shadows, focus color, timings and easing. MudBlazor's palette follows the same colors. The typeface is **Be Vietnam Pro**, bundled locally with its SIL Open Font License from [Google Fonts' source](https://github.com/google/fonts/tree/main/ofl/bevietnampro); system fallbacks remain. The font's [official metadata](https://github.com/google/fonts/blob/main/ofl/bevietnampro/METADATA.pb) lists Vietnamese coverage.

No third-party animation library was installed. `wwwroot/js/motion.js` uses native IntersectionObserver and Web Animations API to animate stable existing wrappers only; it never rearranges Blazor-managed children. `AnimationService` initializes after render and disposes observers/listeners with the layout. CSS handles small feedback, dialog, toast and flashcard reveal transitions. The animation code checks browser/user reduced-motion preferences; content remains visible if the animation module fails. Blazor WebAssembly itself still requires JavaScript. `mobile-drawer.js` manages focus trap, Escape dismissal and focus restore.

## Before and after

| View | Before | After |
| --- | --- | --- |
| Home, 1692px | [image](ui-redesign/before-home-desktop.png) | [image](ui-redesign/after-home-desktop.png) |
| Unit 01, 1692px | [image](ui-redesign/before-unit-desktop.png) | [image](ui-redesign/after-unit-desktop.png) |
| Home, 390px | [image](ui-redesign/before-home-mobile.png) | [image](ui-redesign/after-home-mobile.png) |
| Unit 01, 390px | [image](ui-redesign/before-unit-mobile.png) | [image](ui-redesign/after-unit-mobile.png) |

The screenshots use a fresh Chrome profile and the operating system's dark preference. The final Unit mobile image includes the corrected Vietnamese dropdown. During the automated matrix, light/vi-VN and dark/en-US were both checked, but screenshots were saved only for the four representative views above. Open native select menus, 125%/150% browser zoom, touch ergonomics and screen-reader output were not visually verified.

## Run visual and functional tests locally

```powershell
dotnet restore LinguaFlow.slnx
dotnet build LinguaFlow.slnx -c Release --no-restore
dotnet test LinguaFlow.slnx -c Release --no-build --no-restore
dotnet run --project src/LinguaFlow.Web -c Release --no-build --no-launch-profile --urls http://127.0.0.1:5187
node tools/visual-snapshots.mjs http://127.0.0.1:5187/ artifacts/visual/manual
```

The visual script needs Chrome and Node.js 22. It writes screenshots and `audit.json` to the output folder; `--quick` captures the four representative views only. For the GitHub Pages production path, use [deployment validation commands](PWA_AND_DEPLOYMENT.md), including `validate-publish` and `smoke-publish.mjs` against the actual Release publish.

## Remaining limitations

- The redesign updates shared primitives and targeted surfaces, but manual visual inspection was concentrated on Home and Unit 01. All routes were checked automatically for loading, theme/locale and overflow at 390/1692px; dedicated screenshots of every page and native dropdown popup remain useful.
- Firefox, Edge, Android Chrome and iPhone Safari were not available in this run. Physical touch, PWA installation, device TTS and browser zoom 125%/150% remain unverified.
- The legacy `app.css` still contains older rules and should be consolidated gradually into the new design file after broader manual review. The new file is intentionally loaded last to avoid changing learning behavior while the visual system is migrated.
- A public GitHub Pages deployment was not performed in STEP 14.2. Local data and content formats remain unchanged.
