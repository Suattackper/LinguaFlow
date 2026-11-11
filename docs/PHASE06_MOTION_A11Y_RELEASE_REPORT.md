# Phase 06 — motion, accessibility and regression

## Scope and baseline

The audit named `SOURCE_Work_UIUX_Audit_2026-11-10.md` was supplied in the conversation; that file is not present in this checkout. Its LF-01–LF-11 observations were visually deep for Unit 01 only. This phase changed shared components and automatically discovered all 36 published Units. At baseline, 103 .NET tests passed. The first Chrome traversal found two `h1` elements on the returning-learner Home page. Its 72-state baseline (Home, vocabulary, practice import) had local boot-to-marker median 1499 ms, p95 1878 ms.

## Changes and decisions

| Area / LF link | Change |
| --- | --- |
| Motion and loading, LF-11 | Batch Anime.js entrance scans in one animation frame, shorten entrance to 240 ms, cancel active animations when reduced motion is enabled, remove duplicate exercise-feedback animation. Loading states reserve viewport height to limit shifts. Moving delayed speech controls below the flashcard stopped the observed card shift. No infinite lesson animation was added. |
| Landmarks/headings, LF-02/LF-09 | Reader now has one main landmark from the app shell and a named content region. Returning-learner Home has one `h1`; first-time hero retains its `h1`. |
| Keyboard/focus, LF-05/LF-08 | Quick search traps Tab and restores trigger focus; the UI-language menu supports arrow keys, Escape and focus restoration. JS listeners are disposed. Focus outlines and 44 px header/sidebar controls were standardized. |
| Forms, LF-03/LF-05 | Exercise textarea labels now target unique input IDs rather than article IDs. An obsolete practice-export placeholder was removed. |
| Production regression, LF-01–LF-10 | Browser harness checks 36/36 Unit rendering/navigation and representative UI routes. Production smoke selector follows the current four-step Practice Center. |

Phase 01–05 working-tree changes were retained. The published curriculum JSON, stable IDs, answer keys, grading thresholds, IndexedDB schema and backup format were not changed in this phase; no migration is needed. The 36-row `unit-coverage-matrix.md` documents actual section families, exercise kinds, checklist/navigation, vocabulary/practice coverage and browser status. It does not assert every exercise type was played through in all 36 Units.

## Verification

- `dotnet build LinguaFlow.slnx --no-restore -c Release -v q`: passed, 0 warnings/errors.
- `dotnet test LinguaFlow.slnx --no-restore -c Release -v q`: 52 Core + 39 Web + 12 Converter = 103 passed, 0 failed. E2E xUnit project discovers no tests.
- `node tools/phase01-browser.mjs --phase06 --phase06-units-only --journey-only`: 36/36 Unit routes passed; no JS exceptions. Checked section-next navigation, one visible section, heading/landmark/ID integrity and assessment entry. Output `artifacts/phase01-browser/phase06-units.json`.
- `node tools/phase01-browser.mjs --phase06 --phase06-pages-only --journey-only`: 216 states (9 routes × 6 widths × VI/EN × light/dark) passed before the final loading/flashcard adjustments. A final `--phase06-targeted` run passed Home and Flashcards at all 48 combinations, plus keyboard/reduced-motion checks. Output `artifacts/phase01-browser/phase06-results.json`.
- Production: `node tools/deploy/set-base.mjs src/LinguaFlow.Web/wwwroot/index.html /LinguaFlow/`; `dotnet publish src/LinguaFlow.Web/LinguaFlow.Web.csproj -c Release --no-restore -o artifacts/phase06-publish -v q`; `node tools/deploy/prepare-pages.mjs artifacts/phase06-publish/wwwroot /LinguaFlow/`; `dotnet run --project tools/LinguaFlow.CourseConverter -c Release --no-build -- validate-publish artifacts/phase06-publish/wwwroot /LinguaFlow/`; `node tools/deploy/smoke-publish.mjs artifacts/phase06-publish/wwwroot /LinguaFlow/`: passed. Source base href restored to `/` afterward.

Local headless Chrome at 390 px, light VI measured 0 CLS for sampled Home/course/roadmap/vocabulary/flashcard views after the fixes; earlier flashcard samples showed about 0.07 CLS. The script records FCP and LCP, but those local observations are not Core Web Vitals field data. No INP or Lighthouse score was measured. The 72-state baseline median load was 1499 ms; an earlier post-change comparable run was 1541 ms, so there is no measured speed improvement claim. Sampled light-theme text/background contrast was 14.07:1, muted text/surface 5.59:1 and primary/surface 6.47:1; this is not a complete WCAG contrast audit.

Screenshots actually captured: `artifacts/phase01-browser/phase06-home-mobile.png`, `phase06-import-mobile.png`, `phase06-settings-mobile.png`, `phase06-unit-01-mobile.png`, `phase06-unit-19-mobile.png`, `phase06-unit-36-mobile.png`. The first and last Unit screenshots and Home screenshot were visually inspected. No reference screenshot diff was produced.

## Remaining limits

Confirmed: there is no browser-test discovery in the E2E xUnit project; the Node browser harness supplies the current browser evidence. Potential: network throttling, focus behavior with screen readers, device TTS and browser-specific PWA behavior may differ. Not tested: physical mobile devices, Firefox/Edge, landscape matrix, full screen-reader audit, INP, Lighthouse and the live public deployment. Production smoke uses synthetic isolated learner data and does not modify the user's stored progress. No commit, push or deployment was performed.
