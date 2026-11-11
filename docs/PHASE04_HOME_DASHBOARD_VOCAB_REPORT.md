# Phase 04 — Home, dashboard, roadmap and vocabulary UX

Date: 2026-11-11. Scope: LF-07, LF-09 and LF-10. The named `SOURCE_Work_UIUX_Audit_2026-11-10.md` is absent from this checkout; the audit pasted by the user is the baseline. The uncommitted Phase 01–03 implementation was retained.

## Baseline

- Home placed recommendation and statistics blocks before the learning action for returning learners. The course dashboard led with four metrics. The roadmap displayed 36 flat cards and loaded their real objectives. Vocabulary displayed search plus four selectors and speech controls before the first word card.
- The analytics calculator already derives completion, current Unit and activity from IndexedDB. Course progress records store the last Unit and section. No new progress value or achievement was introduced. Baseline Release tests passed: 52 Core, 36 Web and 12 Converter (100 total). The E2E xUnit project has no discovered tests.

## Decisions and changes

- `CourseResumeResolver` uses the published manifest and stored analytics to select the current unfinished Unit or the first unfinished Unit. A saved section becomes a fragment URL only for that same Unit. When all Units are complete it returns no next lesson. Home and course dashboard show one prominent action before secondary metrics and links; new learners see the actual first Unit on the course dashboard.
- Roadmap groups the 36 published Unit references in ordered sets of six. Each card retains its original title, objectives, activity count and type. A status is shown only from saved progress; untouched Units are available and never falsely locked. Corrupt optional progress summaries are skipped without deleting records. The last section is used in a resume link where valid.
- Vocabulary places Flashcards and Review first, keeps search immediately available, and moves the existing Unit/status/sort/group controls and speech options into an accessible disclosure. A `?unit={unitId}` query is accepted only when that ID exists in the manifest. Search, favorites, queue, review state, paging and TTS logic remain intact.
- Course-context navigation now puts Course, Roadmap, Review and Practice above the unavailable language catalog entries. Help, About and Privacy are grouped as secondary navigation. The compact VI/EN switcher is retained. Shared semantic colors and responsive card/control styles refine the existing design system.
- A reusable skeleton reserves space for Home, course dashboard, roadmap and vocabulary. A Chrome screenshot exposed Home rendering its hero under the skeleton while loading; the hero now waits for successful loading. The first-visit hint was moved below the hero so the primary action appears first.

## Coverage and verification

- `node tools/phase01-coverage.mjs` discovers the 36 published Unit files, section types, authored assessment rules and actual vocabulary entries and writes [unit-coverage-matrix.md](unit-coverage-matrix.md). Data-driven tests cover resume targets for every Unit, an all-completed course, and direct vocabulary queries across all 36 Unit IDs.
- `dotnet build LinguaFlow.slnx -c Release --no-restore` and `dotnet test LinguaFlow.slnx -c Release --no-build --no-restore` are the release checks. Browser checks use `node tools/phase01-browser.mjs --phase04 --journey-only` with a fresh headless Chrome profile. Screenshots and audit JSON are ignored local artifacts under `artifacts/phase01-browser/`.
- Final Release build passed with zero warnings/errors. Automated tests passed: 52 Core, 38 Web and 12 Converter (102 total). The Chrome run passed 60 browser states with no captured exceptions or horizontal overflow: Home and course dashboard at 360, 390, 768, 1024, 1366 and 1920 px in both themes and both UI languages; roadmap in four representative theme/language/width combinations; vocabulary with actual filtered words for Units 01, 19 and 36 on mobile/desktop. A learner opened Unit 19 `#g3` in the fresh test profile; Home then linked back to that same section. After moving the first-visit hint below the hero and adding corrupt-summary handling, a targeted run passed six browser states. The mobile Home, roadmap and vocabulary screenshots were inspected visually.
- An additional rapid favorite/queue interaction found that two updates for the same word could both start from an old state. Vocabulary writes on that page now run in order and read the latest in-memory state. The favorite button also emits an explicit ARIA `true`/`false` value. The final targeted Chrome run passed seven states, including two rapid toggles and a refresh that retained both favorite and review-queue settings. The latest Release build and 102 tests passed after this fix.

## Compatibility and limits

- Course source, published course JSON, answer keys, IDs, grade rules, IndexedDB schema, backups and offline logic are untouched; no migration is required.
- The roadmap still reads all 36 Unit files to show authored objectives and activity counts. This is its existing behavior and can be optimized later without changing the format. Home and course dashboards continue to use analytics summaries and do not load every Unit file.
- Browser widths are emulated in desktop Chrome. Physical Android/iPhone, Safari, Firefox and screen reader use are not claimed as tested.
