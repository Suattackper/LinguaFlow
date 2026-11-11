# Phase 03 — Lesson journey and reader navigation

Date: 2026-11-11. Scope: LF-02, LF-04, LF-10. The named `SOURCE_Work_UIUX_Audit_2026-11-10.md` is absent from this checkout; the user's pasted audit is the baseline. The Phase 01 and 02 work already in the working tree was preserved.

## Baseline and decisions

- The published manifest discovers 36 Unit JSON files. Their authored section counts vary (Unit 01 has 21; representative later and checkpoint Units have 22). Section IDs, titles, blocks and order come from those files. The old reader rendered every section as one continuous long page, used a flat sidebar, and restored only an IndexedDB last-section value. URL fragments did not take precedence.
- `LessonJourneyBuilder` groups the actual ordered sections into Learn, Practice, Test and Review. The grouping is a UI classification of the current published section IDs and response-block types. Unknown future IDs fall back to Learn, keeping their content reachable. It does not change course completion or grading.
- The reader displays one section at a time, with four stage controls, grouped collapsible table of contents, current-section summary, adjacent section actions and a course → Unit → activity breadcrumb. All section components remain mounted but inactive sections are hidden with CSS. This preserves typed drafts and notes when switching sections; print CSS reveals all sections.
- A valid fragment such as `#g3` wins over the saved position. Section actions update the URL fragment and the existing course/unit/section workspace position. Browser back/forward listens for location changes. Scrolling and keyboard focus happen after Blazor renders the newly visible section. Focus Mode retains the reader toolbar and the current section.
- No curriculum, answer key, stable ID, progress record, backup format, offline cache or IndexedDB schema changed. No migration is needed.

## Coverage and verification

- `node tools/phase01-coverage.mjs` passed 36/36 and regenerated [unit-coverage-matrix.md](unit-coverage-matrix.md), including actual section families, paths, exercise kinds, authored rules, stage counts and adjacent Unit navigation. The data-driven Web test checks every section's stage, ordered previous/next links, unique ID and stage entry point across all 36 Unit files.
- `dotnet build LinguaFlow.slnx -c Release --no-restore`: passed with zero warnings and errors.
- `dotnet test LinguaFlow.slnx -c Release --no-build --no-restore`: 52 Core, 36 Web and 12 Converter tests passed (100 total). The E2E xUnit assembly has no discovered test cases; the Node/Chrome browser script supplies browser checks.
- `node tools/phase01-browser.mjs --phase03` passed 41 browser states with zero captured browser exceptions and no horizontal overflow: Unit 01 at 360/390/768/1024/1366/1920 px in VI/EN and light/dark; Units 18 and 36 at mobile/desktop widths; and relevant practice/import/backup/goals/workspace screens. The interaction check navigated stages and sections, used browser back, retained an unsent draft, reopened a fragment URL and toggled Focus Mode. A targeted final run (`--phase03 --journey-only`) passed three states after the mobile checklist width fix, including draft persistence after full page navigation. Screenshots and metrics are in ignored local `artifacts/phase01-browser/`.
- The Chrome screenshot exposed narrow checklist text on mobile inherited from Phase 02. A scoped CSS correction moves the activity action below the condition text; the final targeted browser run verified that condition text has usable width.

## Limits

- Browser checks use headless desktop Chrome with emulated widths, not physical Android/iPhone Safari or a screen reader. The 36-Unit test is data-driven; browser interactions sample representative Units and do not claim a manual journey through every exercise.
- The new journey groups by authored section metadata. If a future curriculum adopts unrelated section IDs, those sections remain accessible in Learn until explicit stage metadata is added to LCF.
