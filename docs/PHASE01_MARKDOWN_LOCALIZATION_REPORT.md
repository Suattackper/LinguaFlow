# Phase 01 — Markdown, localization and design foundations

Date: 2026-11-10. Scope: LF-01 and LF-05, with shared UI foundations only. The audit text was supplied in the conversation. `SOURCE_Work_UIUX_Audit_2026-11-10.md` was not present in the checkout, so the supplied text is the audit baseline. Before this phase, five uncommitted UI fixes from the preceding request existed and were retained.

## Baseline and inventory

- Published course: the manifest discovers 36 Unit references and corresponding JSON files. The source lives in `CourseSources/English/english-life-office-it/`; the converter stores authored Markdown syntax in block `text`/`prompt`/`instructions` and keeps `sourceMarkdown` as provenance. Unit 01 starts with bold labels and an inline-code answer-file reference. No JSON, source lesson, answer key or content fingerprint was modified in this phase.
- Reader text paths: `ContentBlockView` renders explanation, grammar, reading, review, homework, question, activity and example text; `ExerciseRenderer` renders prompts; `OpenAssignment`, `UnitAssessmentSummary` and `ExerciseFeedbackView` render rubrics and explanations. `PracticeCenter` renders task, grammar and rubric text. Dialogue, vocabulary explanation and mistake review use the same authored-text path. User-entered notes, draft answers and imported AI feedback remain encoded plain text.
- `UnitAssessmentSummary` previously stripped `**` and backticks by replacement. The other paths displayed Markdown as raw encoded text. The published JSON was already correctly decoded by `System.Text.Json`; no double escaping or content migration was needed.
- VI/EN resources lacked six static UI keys used in Razor: `All`, `FavoriteWords`, `Filter`, `SaveSettings`, `Search`, `StorageUnavailable`. Native JSON file controls displayed browser-language text. Existing resource counts were 526 per culture; Phase 01 adds these six, two file-picker strings, and five practice-session status strings. The Practice skill selector and Course Progress skill labels now use existing localized skill names.
- The first new coverage test failed because one or more published sections have zero blocks. This is valid input; the test now checks the section heading as a fallback and the matrix records the actual block families.

## Decisions

- `SafeMarkdownRenderer` supports headings, paragraphs, explicit hard line breaks, ordered/unordered lists, strong/emphasis, inline/fenced code and safe links. It constructs Blazor render-tree nodes with `AddContent`; it never injects HTML. HTML from JSON is shown as text. Links allow only `http`, `https`, `mailto` and fragment anchors. Unsupported Markdown remains visible as encoded text rather than being executed.
- A shared `SafeMarkdown` component handles authored strings wherever Markdown is expected. Form labels, learner drafts, imported JSON feedback and course metadata remain plain text. This keeps educational content unchanged and avoids interpreting untrusted AI output as formatting instructions.
- `LocalizedFilePicker` keeps Blazor `InputFile` and its validation callbacks, but supplies a translated visible control and selected-filename status. The underlying browser picker remains native.
- Shared spacing, radii, focus and Markdown typography tokens were added to the existing Step 16 style layer. There is no new package dependency, IndexedDB schema or data migration.

## Coverage and checks

- `node tools/phase01-coverage.mjs` discovers and validates all 36 manifest Unit references. It writes [unit-coverage-matrix.md](unit-coverage-matrix.md) with each Unit's block families, file path, Markdown count, exercise and answer references, adjacent navigation and data/test status. A Web component test renders all authored text fields found in each published Unit section and checks that no script/iframe/object nodes or JavaScript links appear.
- VI/EN resource audit found no missing static `T["..."]` keys after the change; both files contain 539 keys with no duplicate VI keys. Dynamic localization keys still depend on their respective runtime states.
- Release build: passed, zero warnings/errors. Automated tests: 52 Core, 25 Web, 12 Converter passed. The E2E xUnit assembly has no discovered test cases; browser checks use Node/Chrome scripts.
- Headless Chrome: 38 states passed with zero browser exceptions. Unit 01 was loaded at 360, 390, 768, 1024, 1366 and 1920 px in light/dark and VI/EN. Unit 18 and Unit 36 were sampled at 390/1366 px; Practice, assessment import, backup import, Goals and Workspace at mobile VI/light and desktop EN/dark. The checks found rendered `strong` and `code` nodes on Unit 01, no script/iframe/unsafe links, no horizontal overflow or visible Blazor error. Screenshots and metrics are in ignored local `artifacts/phase01-browser/`; this is Chrome emulation, not physical Android/iPhone Safari testing.

## Limits

- The supplied audit's before screenshots are in a separate `sandbox:/workspace/shared/...` location that is not present in this checkout; no before screenshot is claimed here.
- The renderer intentionally supports the Markdown subset used by LCF and this phase's acceptance criteria; arbitrary Markdown extensions and embedded HTML are displayed as text. The exported Markdown practice file remains Markdown by design.
- This phase does not change scoring, completion criteria, learning progress, import/export contracts, offline caching or source curriculum.
