# Phase 05 — Flashcards, ChatGPT Practice, import/export

## Baseline and scope

The repository audit file named in the request is not present in this checkout. The audit text supplied in the conversation identifies LF-05, LF-06, LF-08 and LF-10. Before editing, `dotnet test LinguaFlow.slnx --no-restore -v q` passed 102 tests (52 Core, 38 Web, 12 Converter). Existing practice package schema v1, result validation, confirmation transaction, backup contract, and authored content were retained.

## Changes

- Practice Center now presents four numbered steps. Unit, skill, authored task and rubric are selected from the actual manifest/Unit/assessment data. Long instructions, objectives and rubric live in disclosures. Reusing a previous session selects its export step; creating a new one requires the explicit action. The session remains local and is not marked completed by opening ChatGPT.
- Flashcards honor `?unit=<manifest Unit ID>` in addition to the existing review modes. The Vocabulary Library carries its Unit filter into the flashcard link. Every card has visible progress and an undo action for the most recent review. Undo restores the previous vocabulary state and removes its activity record in one IndexedDB transaction; a final-card session summary is removed in the same transaction when applicable.
- TTS controls explain missing browser capability or missing matching voices and hide an empty voice selector. Existing recording playback and manual refresh remain available.
- Import preview is discarded when pasted JSON changes. The existing validator still rejects malformed/oversized JSON, mismatched session/course/Unit/rubric, unsupported schema, impossible scores and missing evidence; the existing confirmation action alone persists the result. Import and Practice pages show a Unit-aware breadcrumb when a valid Unit query is provided.
- VI/EN strings and responsive stepper styling were added. No published curriculum, answer key, ID, schema version, scoring threshold or PWA cache data was modified.

## Files changed in Phase 05

`src/LinguaFlow.Web/Pages/Flashcards.razor`, `VocabularyLibrary.razor`, `PracticeCenter.razor`, `PracticeImport.razor`, `UnitReader.razor`; `src/LinguaFlow.Web/Components/SpeechControls.razor`, `ContentBlockView.razor`; `src/LinguaFlow.Web/Services/VocabularyLearningService.cs`, `LearningRepository.cs`; `src/LinguaFlow.Web/wwwroot/js/learning-store.js`, `css/step16.css`; both `Strings.*.resx`; `tests/LinguaFlow.Web.Tests/Phase05PracticeCoverageTests.cs`; `tools/phase01-browser.mjs`, `phase01-coverage.mjs`; `docs/unit-coverage-matrix.md` and this report. Other modified working-tree files belong to prior phases and were preserved.

## Coverage and verification

- `node tools/phase01-coverage.mjs`: 36/36 published Units; [unit-coverage-matrix.md](unit-coverage-matrix.md) records per-Unit section types, exercise counts, authored practice references, vocabulary and route scope.
- The new `Phase05PracticeCoverageTests` loads all 36 real Unit JSON files and verifies each has an exportable authored task and matching rubric, an exact Unit scoped flashcard URL, and a self-contained result contract.
- `dotnet build LinguaFlow.slnx --no-restore -c Release -v q`: pass, 0 warnings and 0 errors.
- `dotnet test LinguaFlow.slnx --no-restore -v q`: pass, 103 tests (52 Core, 39 Web, 12 Converter). The E2E assembly reports no discovered xUnit tests; the browser script below supplies browser checks.
- `node tools/phase01-browser.mjs --phase05 --journey-only`: pass, 39 Chrome headless states; widths 360, 390, 768, 1024, 1366 and 1920, alternating VI/EN and light/dark, representative Units 01/19/36. Checks selected Unit, four step navigation, Unit scoped flashcards, horizontal overflow and runtime exceptions. It also creates a practice session, rejects invalid pasted JSON before preview, and grades/undoes a card with IndexedDB record count restored. Browser profiles were temporary; no learner account was modified. Screenshot: ignored `artifacts/phase01-browser/phase05-import-mobile.png`.

## Limits

Actual downloadable Markdown and clipboard permission behavior vary by browser and were not exercised in the automated browser run. A valid external ChatGPT JSON result cannot be generated as ground truth in this local run; the existing Core validator tests cover valid and invalid result contracts. Physical iPhone/Android devices, screen readers, every theme at every viewport, and no-voice hardware were not available. The audit's TTS symptom remains environment-dependent. There is no data migration. No commit, push or deployment was performed.
