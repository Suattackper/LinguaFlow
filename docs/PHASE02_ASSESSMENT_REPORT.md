# Phase 02 — Exercise feedback and Unit assessment

Date: 2026-11-10. Scope: LF-03 and LF-04. The named `SOURCE_Work_UIUX_Audit_2026-11-10.md` is not in this checkout; the audit pasted by the user is the baseline. Phase 01's uncommitted renderer, localization, and design work was retained. No course source, published course JSON, answer key, stable ID, grading algorithm, passing threshold, or IndexedDB schema changed.

## Baseline

- The published manifest discovers 36 Units. Each has seven authored threshold rules. Ordinary Units contain source score placeholders such as `Grammar: **__/23**`; these are worksheet targets, not earned points. Checkpoint Units have explicit numeric thresholds. The course's completion service was not changed.
- The grading service automatically grades only authored auto-choice or auto-exact answers. Free text without a trustworthy key, writing, speaking, and listening require review or external evidence. Existing answer normalization and accepted answers remain intact.
- The reader's existing assessment was in a collapsed detail panel. A submitted manual answer could appear beside a source score placeholder without a clear distinction between earned points and suggested self-checking.

## Changes

- A presentation-only state map distinguishes untouched, editing, submitted, correct, incorrect, authored accepted variant, self-review, external evaluation pending, evaluated, locked, unavailable, insufficient evidence, and partial. It reads existing attempts and never changes their stored grades.
- Exercise feedback now labels automatic scores only when grading actually supplied them. Manual responses say that no automatic score exists, show the authored reference answer after submission, and provide a disclosure for explanations. The next-section action uses the actual section order. Editing a new attempt hides stale feedback until the next submission, while the stored prior attempt remains available after reload.
- The assessment overview shows six skills and a checklist built from each Unit's authored rules. Each activity action resolves to an existing section found in that Unit's typed blocks. Unresolved rules have no dead link. The original detailed rubric remains in a disclosure. Source `__/N` blanks are rendered as “Chưa có điểm” / “No score yet”; no earned result is inferred from them.
- VI/EN strings and the shared component styles cover the new states, feedback, focus, mobile layout, and checklist.

## Validation

- `node tools/phase01-coverage.mjs` discovers all 36 published Units and writes [unit-coverage-matrix.md](unit-coverage-matrix.md). The matrix records actual block families, paths, Markdown render coverage, exercise kinds and answer counts, seven authored rules, checklist availability, and adjacent navigation. N/A denotes absence of an adjacent Unit or exercise kind.
- Data-driven Web tests parse every published Unit, build its checklist against an empty assessment, verify no invented pass and all CTA targets, and construct every response-block definition while checking stable IDs and authored answer mapping. Core tests cover actual grading and answer normalization. The UI state tests verify presentation does not mutate stored results.
- A Chrome script checks Unit 01 at 360, 390, 768, 1024, 1366 and 1920 px across VI/EN and light/dark, samples Units 18 and 36, and checks adjacent practice/import/backup/goals/workspace routes. It submits one authored auto-choice and one manual-review response, then reloads to check persistence. Other Unit render and metadata checks are data-driven rather than 36 individual browser journeys.
- `dotnet build LinguaFlow.slnx -c Release --no-restore` passed with zero warnings or errors. `dotnet test LinguaFlow.slnx -c Release --no-build --no-restore` passed 52 Core, 35 Web and 12 Converter tests (99 total). The E2E xUnit assembly has no discovered tests; the Node/Chrome script is the actual browser suite.
- `node tools/phase01-browser.mjs --phase02` passed 39 browser states with zero captured browser exceptions and no horizontal overflow in the checked widths. The authored auto-choice scored correctly; the manual response showed self-review and no automatic score; both feedback states survived reload. Local screenshots and metrics are under ignored `artifacts/phase01-browser/`. An earlier run completed its checks but Windows locked a temporary Chrome profile during cleanup; the script now treats that cleanup lock as a warning. A run concurrent with a Release rebuild timed out, so the final successful run used the completed build and a restarted server.

## Limitations and follow-up

- The source's ordinary-Unit `/23` and similar placeholders do not define how an external self-check score should be certified. The UI makes that absence explicit; it does not invent a pass rule.
- Browser screenshots and console results are from headless desktop Chrome with emulated widths. Physical iOS Safari, Android Chrome, and screen-reader behavior remain unverified.
- The interaction check does not claim every answer in all 36 Units has been manually submitted. The automated 36-Unit validation covers structure, state mapping, and valid navigation targets.
