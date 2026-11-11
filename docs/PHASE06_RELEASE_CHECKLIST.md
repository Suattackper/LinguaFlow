# Phase 06 release checklist

Checked locally on 2026-11-11. This is a release readiness record, not evidence that the public GitHub Pages deployment was updated.

| Check | Result | Evidence / limit |
| --- | --- | --- |
| Release build and automated tests | PASS | `dotnet build LinguaFlow.slnx --no-restore -c Release -v q`; `dotnet test LinguaFlow.slnx --no-restore -c Release -v q`: 103 passed. The E2E xUnit assembly discovers no tests; browser checks run as Node scripts. |
| All published Units | PASS locally | `node tools/phase01-browser.mjs --phase06 --phase06-units-only --journey-only`: 36/36 opened, rendered, navigated to next section, with assessment entry and no browser exceptions. See `docs/unit-coverage-matrix.md`. |
| Responsive and themes | PASS for tested routes | Nine key routes across 360, 390, 768, 1024, 1366, 1920 px, VI/EN and light/dark: 216 page states, no horizontal overflow or browser exceptions. A final targeted run covered Home and Flashcards at all 48 combinations. Other routes and landscape orientations were not exhaustively checked. |
| Keyboard and reduced motion | PASS for tested controls | Quick search focus trap, Escape and focus restore; language menu keyboard navigation; reduced-motion animation cancellation in headless Chrome. Screen-reader testing is outstanding. |
| Production static publish and offline | PASS locally | Publish with `/LinguaFlow/` base path, `validate-publish`, and `tools/deploy/smoke-publish.mjs`. Smoke covers cached 36 Units, offline lesson/exercise persistence, cache removal retaining IndexedDB, practice export/import. This uses a disposable browser profile. |
| Browser/device support | NOT TESTED | Physical Android/iPhone, Firefox, Edge, browser voice support and assistive technology need device testing. |
| Field performance | NOT TESTED | No Lighthouse or real-user Core Web Vitals run. Headless local FCP/LCP/CLS observations are in the Phase 06 report and are not field metrics. INP was not measured. |

Before publishing, run the release build/test and the production publish smoke above, then check the deployed URL on physical mobile devices. Confirm a nested route refresh, offline lesson, notes, backup export and restore using disposable learner data. Do not clear real IndexedDB data for QA.
