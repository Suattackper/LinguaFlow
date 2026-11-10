# LinguaFlow
A local-first language-learning app built with standalone Blazor WebAssembly (.NET 10). It offers a responsive Vietnamese/English interface, a structured English course, interactive exercises, vocabulary review, local progress, manual ChatGPT practice exchange, backups, approved course downloads, and an installable PWA.

## Features and architecture

English for Life, Office & IT contains 36 original bilingual Units, from A2+ to B2. Learners can read lessons, answer supported graded questions, review vocabulary with spaced repetition and browser speech, keep notes and bookmarks, track real progress, export/import per-course backups, and download the approved Student Book and Answer Key. Japanese and Chinese are coming soon. No account, backend, database server or paid AI API is required.

The solution uses .NET 10, C#, MudBlazor 9.x, static versioned LCF JSON, IndexedDB, .NET localization, the Web Speech API, and a PWA service worker. `LinguaFlow.Core` owns learning logic; `LinguaFlow.Web` owns browser UI and adapters; `LinguaFlow.CourseConverter` publishes developer source Markdown. See [architecture](docs/ARCHITECTURE.md) and [repository structure](docs/REPOSITORY_STRUCTURE.md). Prerequisites are the .NET 10 SDK and a current browser. Node.js 22 and Chrome are needed for the production browser smoke test; the optional `wasm-tools` workload improves publish optimization.

## Run locally

```powershell
dotnet restore LinguaFlow.slnx
dotnet build LinguaFlow.slnx
dotnet run --project src/LinguaFlow.Web/LinguaFlow.Web.csproj
```

Open the localhost address printed by `dotnet run`. No backend is required.

## Course content

The published catalog is at `src/LinguaFlow.Web/wwwroot/Courses/catalog.json`. The format and fingerprint algorithm are documented in [docs/COURSE_FORMAT.md](docs/COURSE_FORMAT.md). To regenerate the published English lessons from the unchanged source files:

```powershell
dotnet run --project tools/LinguaFlow.CourseConverter/LinguaFlow.CourseConverter.csproj -- convert CourseSources/English src/LinguaFlow.Web/wwwroot/Courses docs/COURSE_CONVERSION_REPORT.md
```

The converter publishes structured Units, vocabulary, assessment mappings, practice instructions, and the two approved original course PDFs with a verified ZIP. Static assessment mappings and the Answer Key are publicly inspectable; the unit UI does not display answers before submission. Learner progress is stored in IndexedDB and is never published. Course unit files load when opened. See [the conversion report](docs/COURSE_CONVERSION_REPORT.md) for source coverage and review warnings.

Validate the content with `dotnet run --project tools/LinguaFlow.CourseConverter -- validate src/LinguaFlow.Web/wwwroot/Courses` and compare published Units to original lessons with `node tools/deploy/audit-course.mjs`. To add a course, keep actual source under the matching `CourseSources/{LanguageDirectory}`, publish valid LCF v1 content under `wwwroot/Courses/{LanguageDirectory}/{courseId}`, add a catalog entry, then validate IDs, relative references, fingerprint and approved downloads. To add a learning language, map its code to its directory, provide genuine course content and localized UI labels, then mark it available. See [LCF v1](docs/COURSE_FORMAT.md) and [conversion guidance](docs/COURSE_CONVERSION.md).

## Local data and ChatGPT practice

Progress, attempts, drafts, notes and assessments live in browser IndexedDB. There is no automatic cross-device sync. Export a course progress JSON file and import it on the other device; this is separate from curriculum PDFs and may contain personal notes. See [local storage](docs/LOCAL_STORAGE.md) and [import/export](docs/IMPORT_EXPORT.md).

For speaking or writing practice, select an original task in Practice Center, export its self-contained Markdown file, and attach it to an ordinary ChatGPT conversation. Import the structured JSON result afterward. LinguaFlow matches the saved request, validates rubric and evidence, previews the result, computes its own verdict and persists it locally. Text-only feedback cannot verify pronunciation. No ChatGPT Project or API key is needed. See [practice protocol](docs/CHATGPT_PRACTICE_PROTOCOL.md) and [import rules](docs/CHATGPT_ASSESSMENT_IMPORT.md).

## Offline and deployment

Open a course's **Offline learning** page to download its lesson content. Progress backups remain separate from course downloads. The in-app Help center and [PWA and deployment guide](docs/PWA_AND_DEPLOYMENT.md) cover installation, offline limitations, GitHub Pages setup, and production publish validation. The deployment workflow is at [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml). No public deployment is implied by the presence of the workflow.

## Tests

```powershell
dotnet test LinguaFlow.slnx
```

The Release workflow also runs `tools/deploy/smoke-publish.mjs` against the published static site in headless Chrome. `LinguaFlow.E2E` remains an empty .NET test project.

See [testing](docs/TESTING.md) for production publish verification, [deployment](docs/DEPLOYMENT.md) for GitHub Pages setup, [known limitations](docs/KNOWN_LIMITATIONS.md), and [final release report](docs/FINAL_RELEASE_REPORT.md).

## Author

Nguyễn Trần Anh Tân · born 2003 · Software Engineer.
