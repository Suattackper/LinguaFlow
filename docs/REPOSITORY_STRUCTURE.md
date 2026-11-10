# Repository structure

## Observed on 2026-11-06

```text
LinguaFlow/
  .git/                         Git metadata
  .gitignore                    Visual Studio/.NET ignore rules
  README.md                     Short project description
  CourseSources/
    English/
      english-life-office-it/
        README.md, CURRICULUM.md, STUDENT_BOOK.md
        ANSWER_KEY_BOOK.md, VOCABULARY_BANK.md, GPT-TEACHER.md
        LESSONS/Unit_01.md ... Unit_36.md
        ANSWER_KEYS/Unit_01_Answers.md ... Unit_36_Answers.md
        PDF/StudentBook_Units01-36.pdf, AnswerKey_Units01-36.pdf
        TRACKING/PROGRESS.md
    Japan/                       empty
    China/                       empty
```

The course directory is untracked in Git at this snapshot. Empty directories may not survive a Git clone without a tracked marker; add markers only in a later implementation step if needed.

## Current layout

```text
LinguaFlow/
  LinguaFlow.slnx
  AGENTS.md
  docs/
  CourseSources/English|Japan|China/
  src/
    LinguaFlow.Core/
    LinguaFlow.Web/
      wwwroot/Courses/English|Japan|China/
  tools/
    LinguaFlow.CourseConverter/
  tests/
    LinguaFlow.Core.Tests/
    LinguaFlow.CourseConverter.Tests/
    LinguaFlow.Web.Tests/
    LinguaFlow.E2E/
  .github/workflows/               planned for deployment step
```

As of Step 03, `Courses/catalog.json` and `Courses/English/english-life-office-it/manifest.json` discover 36 structured Unit files, `Vocabulary/vocabulary.json`, `Assessments/assessments.json`, and `Prompts/practice-config.json`. The remaining course subfolders and `Japan`/`China` contain only `.gitkeep` markers. No source progress records or PDFs have been copied into `wwwroot`. See `COURSE_FORMAT.md` and `COURSE_CONVERSION_REPORT.md`.

Use PascalCase for C# projects, public types and the required physical language folders; stable lowercase language codes `en`, `ja`, `zh` in metadata; kebab-case for course IDs; stable, versioned IDs for units and exercises. Preserve existing source filenames and course ID. Keep generated assets distinct from their sources. Use exact casing in URLs and links; GitHub Pages hosting is case-sensitive.
