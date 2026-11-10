# LinguaFlow STEP 15 — UI/UX, navigation and motion

Date: 2026-11-10. This step builds on the existing Step 14 design system. Original course files, published LCF JSON, grading, routes and learner record schemas were not changed. No commit or push was made.

## Audit and page coverage

The baseline and prioritized defects are in [STEP15_UI_AUDIT.md](STEP15_UI_AUDIT.md). The shared design applies to Home, language/course catalog, course dashboard, roadmap, Unit reader and exercises, vocabulary/flashcards/review, Practice Center and result import, progress, backup, offline course, materials, goals, workspace, mistake notebook, Settings, Help, About and Privacy. The existing 21-route Chrome matrix checks loading, language/theme and horizontal overflow. Settings has dedicated screenshots at 390, 768, 1024, 1440 and 1692px in both light and dark mode. Shared controls and layout rules are in `wwwroot/css/step15.css`, after `redesign.css`.

## Changes by phase

| Phase | Result |
| --- | --- |
| A | Read architecture/Step 14 code, inventoried routes and controls, captured baseline screenshots, Release build and 85 tests passed. |
| B | Refined shared spacing, controls, card surfaces, type inheritance, focus and responsive rules. Local Be Vietnam Pro from Step 14 remains the UI font. |
| C | Native selects now reserve a 48px caret zone, use a consistent 46px height, align the chevron and retain readable labels. Mud popovers inherit font, surface and focus styles. |
| D | Action groups wrap with consistent gaps. The isolated Help arrow links were replaced by icon-and-label help entry points. Settings now uses grouped cards and switch controls. |
| E | Desktop sidebar toggles between 252px and 76px, with localized toggle labels, active indicators and title tooltips. Preference uses the existing bootstrap localStorage plus mirrored IndexedDB preference record (`sidebar-collapsed`). Mobile retains the existing overlay drawer and focus trap. |
| F | Settings groups language/region, appearance/accessibility and learning-data reset. Controls retain their existing behavior and confirmation dialogs. |
| G | Shared controls, cards and motion refine the discovered learning and information routes. No learning workflow was rewritten. |
| H | The first-load screen now has a branded mark, short animated progress rail and status text. Existing local `motion.js` remains. CSS adds restrained exercise/flashcard, card and accordion feedback; OS and user reduced-motion settings turn it off. No new motion dependency. |
| I | Existing reader focus mode, Continue Learning and contextual Help remain. The new sidebar preference improves navigation; no unimplemented command palette or fake metric was added. |
| J | Final results and known limits below. |

The sidebar setting initially exposed an invalid IndexedDB record ID during the first Chrome run. The browser showed the startup error; the ID was corrected to `sidebar-collapsed`, then a clean browser startup and preference persistence were verified. This is why the browser pass matters beyond a successful .NET build.

## Visual evidence

- Baseline and after: [Home desktop before](step15-ui/before-home-desktop.png), [Home desktop after](step15-ui/after-home-desktop.png), [Unit mobile before](step15-ui/before-unit-mobile.png), [Unit mobile after](step15-ui/after-unit-mobile.png).
- Settings: [mobile light](step15-ui/settings-light-mobile.png), [desktop dark](step15-ui/settings-dark-desktop.png), [collapsed desktop sidebar](step15-ui/settings-collapsed.png). The complete ten-image viewport/theme set is in `artifacts/visual/step15-final-refresh/`.
- [First-load screen](step15-ui/first-load.png), rendered from the same initial HTML and CSS in Chrome for visual inspection.
- Machine-readable settings/sidebar checks: `artifacts/visual/step15-final-refresh/step15-audit.json` (`mobileLabels: true`, persisted collapsed width 76px).
- Full route matrix: `artifacts/visual/step15-final/audit.json` (`problems: []`).

The screenshots were rendered in headless Chrome using the repository's Chrome DevTools Protocol script. Playwright was not installed in this environment. Native `<select>` popups are drawn by the browser/OS and were not captured by headless page screenshots. The closed control, label width and caret spacing were inspected; native popup keyboard behavior needs a manual check. Actual browser zoom at 125%/150%, screen-reader output and physical iOS/Android touch were not verified here. Native popup styling is browser-dependent.

Final `dotnet restore` succeeded. Release build had 0 warnings/errors; 85 tests passed. Static publish validation and the production browser smoke under `/LinguaFlow/` passed, including service worker, 36 cached Units, offline exercise persistence and manual practice export/result import. The `LinguaFlow.E2E` assembly currently contains no discoverable tests, so the browser coverage comes from the Chrome scripts. Native open-dropdown screenshots and physical browser zoom remain manual checks.

## Local preview recovery

After a later rebuild, the already-running dev server on port 5187 served an HTML import map pointing to `dotnet.kp072tz28n.js` while returning 404 for that runtime file. Blazor never started and the new splash animation remained indefinitely. Restarting that dev server from the synchronized Release build restored the Home and Unit pages; the user's existing Chrome tab was reloaded and showed Home. The splash now presents a localized reload action after 15 seconds if Blazor has still not replaced it. When rebuilding while the local dev server remains open, restart the dev server to refresh its asset manifest.

## Verification commands

```powershell
dotnet restore LinguaFlow.slnx
dotnet build LinguaFlow.slnx -c Release --no-restore
dotnet test LinguaFlow.slnx -c Release --no-build --no-restore
node tools/visual-snapshots.mjs http://127.0.0.1:5191/ artifacts/visual/step15-final --step15 --light
```

The dedicated visual script asserts a 46px Settings select height, no horizontal overflow at the listed widths, theme consistency, a 76px collapsed sidebar and preference persistence after reload. The full matrix checks 21 routes at 390/1692px and Home/Unit at seven widths in both light/vi-VN and dark/en-US. The separate production publish smoke script verifies service worker, project subpath, practice export/import, cached 36 Units, offline exercise persistence and safe content updates.
