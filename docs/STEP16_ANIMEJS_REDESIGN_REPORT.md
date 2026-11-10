# Step 16 — Anime.js redesign and interaction report

Date: 2026-11-10. Scope: the existing standalone .NET 10 Blazor WebAssembly application. No course JSON, source curriculum, IndexedDB schema, learning result, or grading rule was changed. No commit, push or deployment was performed.

## Implemented

| Area | Change |
| --- | --- |
| UI culture | The top bar and Settings now use a compact VI/EN flag control. Only `vi-VN` and `en-US` can be selected; Japanese and Chinese remain learning-language routes, not interface cultures. The existing preference service persists the selection and reloads localized resources. |
| Motion | Anime.js 4.5.0 is bundled locally with its license. A single IntersectionObserver animates existing wrappers in the shell, sidebar, home, course dashboard, roadmap, lessons, exercises, vocabulary, flashcards, Practice Center, Settings, Help and other shared page surfaces. Menu and search panel have short Anime.js entrances. Blazor never loses ownership of DOM nodes. |
| Reduced motion | The motion module reads both the browser media query and the app preference. Existing CSS transitions also stop under either setting. Content remains visible if motion initialization fails. |
| Quick search | `Ctrl+K` (or Command+K) opens a keyboard accessible palette. Static navigation is immediate; available course manifests are loaded on demand to index Unit titles without fetching 36 Unit JSON files. Escape closes it and returns focus; arrows traverse results. |
| Reading focus | The existing Unit Focus Mode now also hides the app sidebar, top bar, footer and breadcrumbs. Exit remains in the sticky reader toolbar. |
| Visual system | Locally hosted SVG flags avoid platform-dependent emoji rendering. The top bar controls, menu and palette share 44px touch targets, focus states, dark surfaces and responsive widths. A route-focused heading no longer shows a decorative focus outline. |

The pre-existing Step 14/15 typography, tokens, card and form designs remain in place. Step 16 CSS is a narrow layer loaded after those styles. The older CSS entrance animations on lesson sections, roadmap cards and practice panels are disabled where the Anime.js observer handles them.

## Validation

- Baseline and subsequent Release builds: successful, 0 warnings and 0 errors.
- Existing automated tests: 85 passed (52 Core, 21 Web, 12 Converter). The E2E assembly currently contains no xUnit test cases; browser verification is run through Node/Chrome scripts.
- Headless Chrome interaction smoke at 390px: language menu had exactly VI and EN; switching to EN persisted; arrow keys moved focus among options and Escape returned focus to the trigger; `Ctrl+K` opened search; Unit 01 was found; Escape closed search and returned focus; Focus Mode hid the shell chrome and retained an exit control; no horizontal overflow or visible Blazor error. The Anime.js bundle loaded from local static assets.
- Four representative screenshots (home/Unit, desktop/mobile), menu/palette screenshots and the interaction audit are saved under ignored local `artifacts/step16-keyboard-final/`.
- Settings was checked in light and dark modes at 390, 768, 1024, 1440 and 1692px with no horizontal overflow or visible Blazor error. The collapsed sidebar and mobile drawer checks passed. Images and metrics are in ignored local `artifacts/step16-settings-matrix/`.
- The full 21-route visual matrix was attempted; it timed out while loading a later route at 1366px, so a complete matrix pass is not claimed. The roadmap still reads 36 Unit files for previews, as it did before Step 16.

## Implementation notes and limitations

- Anime.js is a 118 KB local minified module before HTTP compression. It is loaded only when the application layout initializes, then cached by the PWA asset manifest. No CDN is used.
- Search indexes manifest titles, routes and available courses. It does not index full lesson bodies or vocabulary terms; those have their own existing page search/filter controls.
- Browser key handling uses one early static script so the shortcut survives a full culture reload. The search results remain usable if the motion import fails.
- A local Release publish was validated under `/LinguaFlow/`. The published browser smoke passed service worker startup, subpath routes, VI/EN switching, practice export/import, 36 cached Units, offline lesson/exercise persistence and compatible course cache update/removal. This validates a local GitHub Pages-shaped artifact; no public deployment occurred in Step 16.
- Real device checks on Android/iPhone, screen reader output and physical touch remain useful manual follow-up.

## Follow-up UI correction

After visual review, the top-bar and sidebar entrance animations were removed so the existing width transition remains the only sidebar motion. The duplicate centered LinguaFlow wordmark was removed, and the sidebar toggle moved into the sidebar header with a compact chevron. The command palette now paints above all cards and selectors through an explicit top-bar stacking layer.

All 16 native `<select>` controls used by Settings, Practice, Vocabulary, Goals, Workspace, Mistakes and speech settings were replaced by the shared `AppSelect` component. Its trigger displays one selected label, while the menu exposes the available choices. The VI/EN control keeps flags and abbreviations but shares the same visual language. Both selectors support pointer, Enter/Space, arrow keys and Escape. Menu placement flips upward near the bottom of the viewport. This is a presentation change; selection values and learning logic remain the same.

The first control audit found a real 451px layout viewport on a 390px Vocabulary screen caused by the voice selector. Responsive speech-control sizing corrected it. Subsequent 390px light/VI and 1692px dark/EN checks of Settings, Practice, Vocabulary, Goals and Workspace found no horizontal overflow or menu clipping. A component test verifies selection callbacks and the single-value trigger.

After this correction, the Release build completed with 0 warnings and 0 errors; 86 tests passed (52 Core, 22 Web, 12 Converter). The updated production publish was validated under `/LinguaFlow/`. Its browser smoke passed route navigation across five viewport widths, VI/EN switching, practice export/import, 36 cached Units, offline lesson/exercise persistence, and compatible cache update/removal. A broader local route screenshot sweep was stopped after it stalled at a later desktop route; the completed control matrix and production smoke are the verified browser coverage for this correction.

## Local commands

```powershell
dotnet build LinguaFlow.slnx -c Release --no-restore
dotnet test LinguaFlow.slnx -c Release --no-build --no-restore
dotnet run --project src/LinguaFlow.Web -c Release --no-build --urls http://127.0.0.1:5191
node tools/visual-snapshots.mjs http://127.0.0.1:5191/ artifacts/step16-check --quick --step16
```

For project subpath publishing, follow `docs/PWA_AND_DEPLOYMENT.md`. Restore the source `index.html` base href to `/` after local publish validation.
