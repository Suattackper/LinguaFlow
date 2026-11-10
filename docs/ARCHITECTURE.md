# Planned architecture

## Boundaries

| Project | Responsibility |
|---|---|
| `LinguaFlow.Web` | Standalone Blazor WASM UI, routing, MudBlazor 9.x, localization, browser JS interop, static asset loading, PWA |
| `LinguaFlow.Core` | Strongly typed course/progress/attempt models, validation, grading, rubric and spaced-repetition logic; no browser dependency |
| `LinguaFlow.CourseConverter` | Developer CLI that reads `CourseSources` and writes validated, versioned JSON/public documents after review |
| `LinguaFlow.Core.Tests` | Pure model, validation, grading, migration-contract tests |
| `LinguaFlow.Web.Tests` | Component states, localization, accessibility-relevant interaction tests |
| `LinguaFlow.E2E` | Browser tests for key learning, persistence, import/export, offline and Pages-base-path flows |

`Web` and `CourseConverter` depend on `Core`; test projects depend on their subjects. `Core` must not depend on `Web` or the converter. No server runtime is planned.

## Content and local state

`CourseSources` is immutable input. The converter creates a catalog plus course-scoped static JSON with schema version, language code (`en`/`ja`/`zh`), course ID, unit IDs, exercise IDs, rubric definitions, and links to approved downloads. Validate ID uniqueness, references, answer coverage and content provenance. Use `English`/`Japan`/`China` only for physical paths. Published content is public: assume any answer key shipped to the browser can be read by learners; interaction may delay reveal but cannot secure it. Review the original progress template and teacher instructions before copying any file into `wwwroot`.

IndexedDB stores local user records keyed by course and stable content IDs. Use versioned records and migrations. Export/import per-course learner backups through an explicit, validated file format; preview conflicts and avoid silent overwrites. Course PDFs/downloads are public content and never constitute progress backups. Static hosting cannot provide cross-device sync or server recovery.

## Learning and assessment

Grade deterministic question types in `Core` using normalized, documented rules. Use authored rubrics for open-ended work; distinguish learner self-assessment from imported ChatGPT feedback and from the app's independently calculated verdict. Store evidence and rubric dimensions with attempts. A normal ChatGPT conversation receives a self-contained generated package; imported JSON is untrusted input, so validate schema/version, course/unit/exercise IDs, score bounds and provenance before saving. No direct ChatGPT synchronization. Do not infer audio quality, listening performance or pronunciation from a text transcript.

## Browser and deployment

The Web Speech API provides TTS where supported, with explicit fallback states. Any later microphone feature needs separate browser permission and capability checks. PWA service-worker caching must use versioned assets and avoid caching private exports as public files. Resolve JSON, navigation and download URLs relative to the GitHub Pages repository base path; test a subpath deployment. Use `.resx`-based .NET localization for interface strings, default `vi-VN`, alternative `en-US`. Design tokens and MudBlazor themes provide light/dark modes; keyboard and screen-reader behavior are acceptance criteria.
