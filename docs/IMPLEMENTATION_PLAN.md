# Implementation plan

Step 00 records the repository audit and project contract only. Do not treat planned features as implemented.

1. **Foundation:** create solution and the six projects in `REPOSITORY_STRUCTURE.md`; target .NET 10; add pinned, compatible MudBlazor 9.x and test dependencies; establish CI build/test and GitHub Pages base-path strategy. Verify package versions at implementation time.
2. **Course model and conversion:** define versioned JSON schema, stable IDs, catalog/manifest, validation and converter CLI. Map the existing English course into typed lessons/exercises/rubrics without inventing content. Review generated assets and answer-key access. Add converter/domain tests.
3. **App shell:** navigation, responsive accessible layout, design tokens, light/dark themes, `vi-VN`/`en-US` resources, catalog and coming-soon language states.
4. **Learning flow:** course dashboard, roadmap, lesson display, deterministic exercises, feedback and rubric-based open-ended work. Make unavailable modes explicit until implemented.
5. **Local learning data:** IndexedDB schema and migrations, attempts, progress, notes, bookmarks, mistakes, vocabulary and spaced repetition; per-course backup export/import with validation and conflict handling.
6. **Practice and media:** Web Speech TTS with capability/error states; listening and speaking paths based on actual browser capability. Generate self-contained ChatGPT practice packages for normal conversations; validate imported assessment JSON and independently calculate rubric verdict before saving.
7. **Finishing and release:** course document downloads, guide and factual About page, PWA caching/offline checks, accessibility and browser tests, GitHub Pages deployment workflow, release verification.

At every step, read `AGENTS.md`, inspect current implementation, implement only that step, test what changed, and update decisions. Future step boundaries can be refined when requested. Do not copy the source `TRACKING/PROGRESS.md` into published course assets: learner state belongs in IndexedDB and explicit backup files.

## Step 00 audit snapshot (2026-11-06)

- Branch `main` tracks `origin/main`; `CourseSources/` is untracked. No other tracked changes appeared at audit time.
- Root contains `.git`, `.gitignore`, `README.md`, and `CourseSources`; no solution, source projects, test projects, workflow, or published assets yet.
- .NET SDK 10.0.402 and standalone `blazorwasm` template are available; no .NET workloads are installed. Build/tests are unavailable until projects exist.
- English source has one nested course, `english-life-office-it`, with 36 lesson files, 36 per-unit answer files, two PDFs, combined books, curriculum, vocabulary bank, teacher guide and progress template. `Japan` and `China` directories exist but contain no course files.
