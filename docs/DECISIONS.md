# Architecture decisions

## D001 — Static, local-first application

**Status:** accepted by product requirement, 2026-11-06. Use standalone Blazor WASM on .NET 10, GitHub Pages, static JSON, and IndexedDB. There is no backend, server database or required account. Consequence: data stays in the browser unless the learner exports a backup; the UI must explain storage limitations and never silently reset data.

## D002 — Source/published content boundary

**Status:** accepted. Preserve `CourseSources` and generate reviewed assets under `src/LinguaFlow.Web/wwwroot/Courses`. Physical folders are `English`, `Japan`, `China`; metadata codes are `en`, `ja`, `zh`. Consequence: converter validation and exact-case URL tests are required. Static answer keys cannot be kept secret from users.

## D003 — Normal ChatGPT conversation exchange

**Status:** accepted. Generate a portable, self-contained practice file, then import structured assessment JSON from a normal conversation. Validate input and compute a local rubric verdict. Consequence: no direct sync, API integration, ChatGPT Project, Custom GPT or API key is implied.

## D004 — Project separation

**Status:** planned. `Core` owns pure domain logic; `Web` owns UI/browser adapters; `CourseConverter` owns source transformation; three test projects cover domain, components and browser flows. This keeps content conversion and assessment logic testable without a browser.

## D005 — Educational evidence and honest capability states

**Status:** accepted. Deterministic exercises use authored answers. Open work uses authored rubrics and identified evidence. Do not auto-certify CEFR achievement or grade listening/pronunciation without appropriate audio evidence. Source content describes B2 as a goal, not a guaranteed outcome.

## Open decisions for implementation steps

- Exact JSON schemas, converter mapping and answer-key publication policy after a full content audit.
- IndexedDB adapter/package choice, record schema and migration policy.
- Component and browser test frameworks, pinned compatible package versions.
- GitHub Pages deployment path and PWA update strategy after the target repository URL is known.

## D006 — Step 01 interface culture and preferences

**Status:** implemented, 2026-11-09. The app defaults to `vi-VN` regardless of browser language. It persists UI language, theme and accessibility preferences in browser local storage. `.resx` resources hold global interface strings. Language changes reload the current route so all page components receive the selected culture. The WebAssembly project loads globalization data for dynamic culture selection. This preference store does not hold learning progress; IndexedDB remains planned for that data.

## D007 — Foundation dependency versions

**Status:** implemented, 2026-11-09. Step 01 pins MudBlazor 9.0.0, Microsoft.Extensions.Localization 10.0.13, bUnit 2.4.2 and AngleSharp 1.5.0. The latter overrides an older transitive version with a reported vulnerability. Reassess versions when adding future dependencies.

## D008 — LCF v1 static content boundary

**Status:** implemented, 2026-11-09. Core owns typed LCF v1 models, path and reference validation, and deterministic SHA-256 fingerprinting. Web services fetch catalog and manifest first; units load only when opened and are cached per app session. The first publisher turns the existing 36 English lesson Markdown files into source-preserving, escaped text blocks. It does not publish answers or learner progress. See `COURSE_FORMAT.md` for the format contract and extension rules.

## D009 — Original curriculum conversion

**Status:** implemented in Step 03. The converter now lives in `tools/LinguaFlow.CourseConverter`. Individual lesson and answer files take priority; combined books and Curriculum are cross-checks. Generated Units retain full original section Markdown and source-relative provenance alongside typed blocks and stable IDs. Vocabulary, answer mappings, rubrics, thresholds, review schedules, and teacher instructions are separate versioned LCF payloads. Static answer data is public even though the unit UI does not fetch it. Alternative or multi-blank answers remain manual-review candidates. The report records exact conversion counts and warnings; no original source file is changed.

## D010 — Step 04 reader state

**Status:** implemented in Step 04 and extended in Steps 05–06. The reader loads published LCF units and renders authored content. Roadmap previews load units only when that route opens. `IReaderWorkspace` retains last section, drafts, bookmarks and notes in versioned IndexedDB. Section position is a reading location, not a completion or grade. Step 06 loads public assessment keys when a Unit opens but hides them in the UI until submission.

## D011 — IndexedDB transaction and backup boundary

**Status:** implemented in Step 05. One browser ES module owns IndexedDB transactions; Web components call C# repositories only. Course progress backup v1 contains learner records scoped to one course and exactly one content version/fingerprint. Replace and its pre-import snapshot share a single transaction; invalid/incompatible files are rejected before writes. No implicit migration or merge is provided. UI preference bootstrap remains in localStorage and mirrors to the repository. See `LOCAL_STORAGE_AND_BACKUPS.md`.

## D012 — Authored grading boundary

**Status:** implemented in Step 06. Only explicit, unambiguous answer metadata enables automatic grading. Existing source conversion permits 36 single-choice questions; all other original answers remain manual review. Test mode hides UI solutions until submission but cannot conceal static answer data from a technical user. Every attempt and mistake is stored under its course ID in IndexedDB. Original threshold and rubric lines are displayed; insufficient evidence prevents false Unit completion. See `ASSESSMENT_ENGINE.md`.

## D013 — Vocabulary review and browser speech

**Status:** implemented in Step 07. Published LCF vocabulary remains the source of truth. Per-word review state and review sessions use the existing course-scoped IndexedDB repositories and backup format. Unit-specific review intervals override the 1/3/7/14-day default; due instants are UTC and calendar summaries use local dates. Browser Web Speech is an optional adapter with saved voice/rate preferences. Supplied audio takes precedence, and playback evidence is never treated as comprehension. See `VOCABULARY_AND_SPEECH.md`.

## D014 — Manual ChatGPT practice package

**Status:** implemented in Step 08. The site exports one versioned Markdown request per explicitly created practice session. Authored LCF task, rubric, vocabulary, grammar, and course teacher instructions are serialized as data inside the package. The package and session ID are saved in IndexedDB before download. The v1 result contract is fixed in Core for STEP 09 to validate and score independently. The browser only opens an ordinary ChatGPT URL; no API or sync is involved. See `CHATGPT_PRACTICE_PROTOCOL.md`.

## D015 — External assessment import boundary

**Status:** implemented in Step 09. Imported JSON must match an IndexedDB request and its frozen rubric, then the current manifest fingerprint. Preview is read-only and confirmation revalidates before an atomic two-record write. Distinct attempts remain in history and identical imports are detected by a canonical result hash. Pass decisions require an authored threshold for that exact skill; the current English open-skill rubrics have weights but no numeric pass threshold. External AI evidence and feedback remain unverified observations. See `CHATGPT_ASSESSMENT_IMPORT.md`.

## D016 — Course-scoped learning analytics

**Status:** implemented in Step 10. Core calculates metrics and deterministic recommendations from saved learning records. Web reads one course at a time from IndexedDB and only catalog/manifest/vocabulary static files for dashboard data. Completion requires saved Unit completion status; activity events cannot imply it. Study duration is recorded only by explicit finish after visible focused time. Goals and personal workspace remain course-scoped and participate in the existing backup. See `LEARNING_ANALYTICS.md`.
## D017 — PWA shell and explicit course caches

The Release service worker caches versioned application assets and the small catalog from Blazor's asset manifest but excludes Unit content and downloads from install-time pre-caching. Course offline downloads are explicit and validated by the LCF fingerprint before a complete cache switch. Course cache removal never touches IndexedDB. Changed IDs, exercise content or scoring rules have no defined migration, so updates are rejected while the existing cache and progress remain. The development worker stays network-only to avoid stale local builds. GitHub Pages base href is set before publish, and a generated 404 redirect supports deep links without server rewrites.

