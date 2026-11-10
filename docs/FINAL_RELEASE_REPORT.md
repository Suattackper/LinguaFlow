# LinguaFlow final release QA report — 2026-11-10

## Executive summary

**Status: release candidate; final acceptance remains blocked by uncompleted full learner-journey and physical-device verification.** The .NET 10 standalone Blazor WebAssembly app builds and publishes, 84 automated .NET tests pass, the public static tree validates, and a fresh headless Chrome profile completes a production subpath/offline smoke test. This report distinguishes tested behavior from features covered only by source inspection or isolated tests. No public GitHub Pages deployment was performed or confirmed.

## Architecture and implemented features

`LinguaFlow.Core` holds LCF contracts, validation, grading, practice/rubric rules, backups and analytics. `LinguaFlow.Web` holds the responsive localized UI, static HTTP course loader, IndexedDB adapter, speech and PWA adapters. `LinguaFlow.CourseConverter` publishes developer Markdown into static LCF v1. No backend or mandatory account is present. The app includes course catalog/roadmap/reader, deterministic supported exercises, vocabulary review, notes/bookmarks, dashboards/goals, per-course backup, manual ChatGPT package/result exchange, approved materials downloads, help/about/privacy, and an offline manager.

## Original course coverage

The converter report records 81 discovered source files (79 Markdown, 2 PDF), 36 individual lessons and answer files, 36/36 converted Units, 760 sections, 858 numbered exercises, 720 vocabulary entries, 112 grammar blocks, 36 reading blocks, 66 writing tasks, 72 speaking tasks, 36 listening tasks, 36 homework blocks and 72 rubric sections. It flags 52 ambiguous/multipart answer mappings for manual review. An independent Step 13 script compared all 36 published Unit titles, IDs, order, objective presence, section provenance and duplicate block/exercise IDs with individual source lessons; it passed. Representative Units 01, 12, 18, 30 and 36 retain standard, checkpoint, integrated assessment and capstone distinctions. The audit is structural; it does not assert that every extracted answer or explanation has been manually proofread. See [conversion details](COURSE_CONVERSION_REPORT.md) and [audit command](TESTING.md).

## Build and automated tests

| Check | Result |
| --- | --- |
| `dotnet build LinguaFlow.slnx -c Release --no-restore` | Passed; 0 warnings, 0 errors |
| `dotnet test LinguaFlow.slnx -c Release --no-restore` | Passed; 52 Core, 20 Web, 12 Converter tests; 0 failed |
| `LinguaFlow.E2E` .NET project | No tests discovered; browser test lives in a Node script |
| `dotnet publish` Release, base `/LinguaFlow/` | Passed; optional `wasm-tools` unavailable, published without its optimization |
| `validate-publish` on actual `wwwroot` | Passed |
| `node tools/deploy/audit-course.mjs` | Passed; all 36 source-to-output title/ID checks |
| `dotnet list ... package --vulnerable --include-transitive` | No vulnerable packages reported by configured NuGet sources on 2026-11-10 |

## Browser test coverage

The production `smoke-publish.mjs` ran in a **fresh temporary headless Chrome profile** under `/LinguaFlow/`. It verified Vietnamese initial `lang`, switched to English and back, opened language/course/roadmap/Unit 01/practice/materials/help/about/privacy routes, checked horizontal overflow at 360/390/768/1024/1440 px, loaded the service worker and manifest, fetched all declared downloadable resources and checked declared sizes, created and downloaded a Unit 01 Speaking practice package, imported a deterministic matching assessment JSON through the UI, confirmed it, and checked IndexedDB persistence after page refresh. It also cached all 36 Units, detected a compatible content update, rejected changed exercise content, opened Unit 01 offline, submitted an exercise offline, observed its IndexedDB attempt, and removed offline course content while retaining the attempt. It did not install the PWA through a browser prompt or run on physical phones. Browser testing was limited to headless desktop Chrome; Firefox, Edge, Android Chrome and iPhone Safari were not exercised.

The full 35-step learner journey was **not completed as one browser transaction**. Vocabulary TTS/flashcard, correct/incorrect grading plus mistake review, and backup export → reset → import → restore remain to be exercised in the browser. Isolated .NET tests cover backup validation/round trip, grading and vocabulary rules, but they are not a substitute for these UI journeys. No live external AI was called; the import used a deterministic local fixture.

## Defect fixed during Step 13

When catalog loading failed on Home, the page previously fell through to the first-visit onboarding state. Home now shows a friendly load error and retry action. This prevents a network/content failure from appearing as an empty account. The Release build and production browser smoke passed after the change. The conversion report was also corrected to distinguish lesson conversion from later approved PDF publishing.

## Release checklist

| Area | Status | Evidence or outstanding work |
| --- | --- | --- |
| Build and static publish | Passed | Release build/publish and output validator |
| Automated .NET tests | Passed | 84 passing tests; E2E .NET project empty |
| Course data | Passed with review warnings | 36 structural source comparisons; 52 ambiguous answers await educational review |
| Exercise engine | Passed in isolated tests and Chrome smoke | Supported grading test suite; one actual offline submission |
| Vocabulary and TTS | Not tested end to end | Vocabulary unit tests exist; physical/browser voice behavior unverified |
| Speaking and ChatGPT file exchange | Passed for manual file protocol in Chrome | Package export and matching deterministic result import/refresh passed; actual ChatGPT conversation not tested |
| Progress persistence | Passed for tested case | Offline exercise attempt retained in IndexedDB after cache removal |
| Backup export/import/reset | Not tested end to end | Validation/round-trip unit tests pass; browser transaction pending |
| Approved downloads | Passed in Chrome | Published files resolve under subpath and match manifest sizes |
| Localization | Passed for switch | Clean vi-VN default and English/Vietnamese switching in Chrome; full copy review pending |
| Accessibility | Not tested comprehensively | Semantic/focus components exist; keyboard and screen-reader audit pending |
| Responsive UI | Passed for overflow check | Five emulated widths; touch and physical-device usability pending |
| PWA/offline | Passed in Chrome | Service worker, 36 cached Units, offline lesson/exercise, update safety |
| Deployment | Not tested publicly | GitHub Actions workflow exists; no public run/site verified |
| Documentation | Passed for required files | README and requested operational guides present; manual walkthrough still useful |

## Security, privacy and limitations

Course JSON, answer files and approved PDFs are public static assets; answer secrecy cannot be guaranteed. Imported AI JSON is untrusted, matched to a saved request and stored rubric, and shown as external AI feedback. A text transcript cannot establish pronunciation or audio listening evidence; an imported JSON file does not authenticate an actual conversation. Learner records live in IndexedDB and are not published with `wwwroot`; exported backups can include personal notes and should be kept privately. Clearing site data or browser eviction can remove local progress. Offline course removal does not remove progress. The published files exclude developer tracking and source directories according to the publish validator. Known functional limits are listed in [KNOWN_LIMITATIONS.md](KNOWN_LIMITATIONS.md).

## Remaining release blockers and manual verification

Complete the pending browser journey on a clean profile: graded correct/incorrect answers and mistake notebook; vocabulary TTS and flashcard; export progress; reset with confirmation; restore that backup and compare attempts, notes and result history. Review the Speaking result page's rubric and evidence text manually even though the deterministic import passes automation. Then test keyboard-only and screen-reader navigation, downloads/uploads and installation/offline behavior on the target mobile browsers. Verify the public GitHub Pages Actions deployment and deep links before claiming the site is live. If these checks expose defects, fix them and rerun the Release build, 84-test suite, output validator and browser smoke.
