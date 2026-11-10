# LinguaFlow Course Format (LCF) v1

LCF v1 is a static, versioned JSON format for public course content. JSON property names use camelCase. The application loads `Courses/catalog.json` relative to its deployment base URL; it never assumes a root-relative `/Courses` URL. All files are UTF-8. `schemaVersion` is the integer `1` in catalog, manifest and unit files. Unsupported schema versions are rejected.

## Layout

```text
wwwroot/Courses/
  catalog.json
  English/<course-id>/manifest.json
  English/<course-id>/Units/<unit-id>.json
  English/<course-id>/Vocabulary/
  English/<course-id>/Assessments/
  English/<course-id>/Prompts/
  English/<course-id>/Assets/
  English/<course-id>/Downloads/
  Japan/
  China/
```

Each course belongs to exactly one directory. `languageCode` is `en`, `ja`, or `zh`; the corresponding directory is **English**, **Japan**, or **China**. Paths and casing are exact. Course IDs, unit IDs, section IDs, block IDs and exercise IDs use lowercase letters/digits separated by hyphens and must remain stable for the life of that content identity.

## Catalog

`catalog.json` has `schemaVersion` and a `courses` array. Each entry requires `courseId`, `languageCode`, `languageDirectory`, `title`, `description`, `level`, `status`, and `manifestPath`; `coverImage` and `tags` are optional. `status` is `available`, `coming-soon`, or `archived`. Only `available` courses can open a manifest or unit. The manifest path must be exactly `Courses/<languageDirectory>/<courseId>/manifest.json`. A cover path, when present, stays in that course's `Assets/` directory. Unknown optional catalog metadata is ignored.

Example:

```json
{
  "schemaVersion": 1,
  "courses": [{
    "courseId": "english-life-office-it",
    "languageCode": "en",
    "languageDirectory": "English",
    "title": "English for Life, Office & IT",
    "description": "Bộ giáo trình riêng cho người học Việt Nam làm công việc phần mềm.",
    "level": "A2+ → B2",
    "status": "available",
    "manifestPath": "Courses/English/english-life-office-it/manifest.json"
  }]
}
```

## Manifest

Vocabulary entries may additionally provide `partOfSpeech`, `example`, `explanation`, `topic`, and `audioPath`. These fields are optional and must come from authored content. An `audioPath` is a safe course-relative path inside `Assets/`; the UI prefers that recording and can fall back to browser speech. Stable `vocabularyId` and `unitId` remain required for review records.

Required fields: `schemaVersion`, `courseId`, `contentVersion`, `contentFingerprint`, `languageCode`, `title`, `subtitle`, `description`, `learningLevel`, `units`, `vocabulary`, `assessments`, `aiPractice`, `downloads`, and `completionRules`. `mediaAssets` is optional. Each unit reference has `unitId`, `title`, positive unique `order`, and a path under `Units/`. Other references have stable `id` and path under their matching folder. Downloads also have `title`. Empty reference arrays are valid; they do not claim an unimplemented activity exists. `completionRules.kind` is `all-units` or `minimum-units`; the latter also requires a valid `minimumCompletedUnits`.

Each published download has `id`, `title`, `description`, `category` (`student-book`, `answer-key`, `supplementary`, `full-package`), `format` (`pdf`, `md`, `zip`), `path`, optional `fileSize` in bytes, `sourceLanguage`, nonnegative `displayOrder`, and `available`. The path stays under `Downloads/` and is resolved from the manifest URL. The publisher checks file presence, size, PDF signature and ZIP integrity. Download bytes do not enter the learning-content fingerprint because they do not affect progress identity; `contentVersion` and fingerprint still describe the structured curriculum.

All manifest child paths are **relative to the manifest URL**. A unit path is `Units/unit-01.json`, for example. The resolver rejects absolute URLs, leading slashes, backslashes, query/fragment suffixes, encoded traversal, `.` and `..` segments, and paths outside the course. Missing files return a clear load error when requested.

`contentVersion` is a human-assigned content release version. `contentFingerprint` is computed, never typed manually: sort all published course payload files referenced for fingerprinting by relative path using ordinal comparison; for each file append UTF-8 path bytes, one zero byte, invariant decimal byte length, one zero byte, then the exact file bytes to SHA-256. Encode lowercase hex with `sha256:` prefix. The manifest and catalog are excluded to avoid self-reference. The Step 03 converter fingerprints all 36 unit files plus `Vocabulary/vocabulary.json`, `Assessments/assessments.json`, and `Prompts/practice-config.json`. A content change requires regeneration of the fingerprint and manifest.

## Unit files and blocks

Each unit file requires `schemaVersion`, `courseId`, `unitId`, `title`, and `sections`. Each section has `sectionId`, `title`, and `blocks`; IDs are unique within their unit. Every block has `type` and `blockId`, optional `optional` (default false), and optional `localizedText` map (culture code to source-authored text). Text found in course sources is retained as content and is independent of the interface locale.

The explicit `type` discriminators are `heading`, `explanation`, `grammar`, `example`, `dialogue`, `reading`, `vocabulary`, `question`, `exercise-group`, `writing`, `speaking`, `listening`, `homework`, `review`, `image`, and `audio`. Text families require `text`; question/writing/speaking/listening require `exerciseId` and `prompt`; exercise group requires `exerciseId` and `title`; image requires `path` and `alt`; audio requires `path` and `transcript`. Media paths remain inside the course's `Assets/` folder. Unknown blocks marked `optional: true` are skipped with a warning; unknown required blocks reject the unit. Required properties, duplicate IDs, mismatched course references, invalid paths and unsupported schemas reject the file.

The Step 03 converter preserves each original Markdown section in `sourceMarkdown` and maps authored grammar, examples, vocabulary, numbered questions, reading, writing, speaking, listening, homework, and review instructions to typed blocks. Sections retain source order, original heading, stable source section ID, and optional parent section ID. Units add source-authored objectives, review schedule days, a checkpoint/capstone distinction, and source provenance. Blocks add source provenance; question blocks may include original options and response kind. Public source references are course-relative (`LESSONS/...`, `ANSWER_KEYS/...`, etc.) and contain no workstation path.

`Vocabulary/vocabulary.json` contains indexed vocabulary entries with term, IPA, Vietnamese meaning, optional context and Unit/source IDs. `Assessments/assessments.json` contains source answer text, answer explanations where authored, assessment mode (`auto-choice` or `manual-review` in the current conversion), rubric sections, and progression threshold lines. Vocabulary responses remain manual-review because the source permits equivalent wording; only unambiguous A/B/C keys are marked as automatic-grading candidates. It is a **public static file**, not a secure answer service; the Step 06 reader fetches it when a Unit opens but does not display answers before submission. Automatic grading was not implemented in Step 03. `Prompts/practice-config.json` preserves the teacher instructions and evidence rules. Course JSON and Markdown are data only: never execute HTML or scripts from them. The unit reader renders escaped plain text; any future rich-text rendering must sanitize input.

Step 06 adds optional answer metadata `validAnswers` (explicit alternatives), `allowPartialCredit`, and `ignorePunctuation`. These fields are never inferred from a slash-separated source answer. The current published course uses its original Step 03 answer policy; 36 unambiguous `auto-choice` questions can be graded automatically, while remaining answers are shown for review after submission. See `ASSESSMENT_ENGINE.md`.

## Current publisher

From the repository root:

```powershell
dotnet run --project tools/LinguaFlow.CourseConverter/LinguaFlow.CourseConverter.csproj -- convert CourseSources/English src/LinguaFlow.Web/wwwroot/Courses docs/COURSE_CONVERSION_REPORT.md
```

The converter recursively discovers the English source course under `CourseSources/English`, prefers individual lessons and answer keys, and cross-checks the combined books and Curriculum. It writes known generated files only, after validating the in-memory output; it never deletes other published files or modifies `CourseSources`. `ANSWER_KEYS` are converted to the public assessment JSON, not copied as Markdown. Step 11 publishes only the two approved original PDFs and their verified ZIP. `TRACKING/PROGRESS.md`, developer instructions, and private learner data are never published. Additional source course layouts need review before conversion.

```powershell
dotnet run --project tools/LinguaFlow.CourseConverter/LinguaFlow.CourseConverter.csproj -- validate src/LinguaFlow.Web/wwwroot/Courses
dotnet run --project tools/LinguaFlow.CourseConverter/LinguaFlow.CourseConverter.csproj -- rebuild CourseSources/English src/LinguaFlow.Web/wwwroot/Courses docs/COURSE_CONVERSION_REPORT.md
dotnet run --project tools/LinguaFlow.CourseConverter/LinguaFlow.CourseConverter.csproj -- report CourseSources/English docs/COURSE_CONVERSION_REPORT.md
dotnet run --project tools/LinguaFlow.CourseConverter/LinguaFlow.CourseConverter.csproj -- publish-materials CourseSources/English src/LinguaFlow.Web/wwwroot/Courses
```
