# LinguaFlow project contract

Read this file before editing code. Implement only the requested step. Inspect existing work before replacing it, preserve completed behavior, and do not commit or push unless explicitly requested.

## Product

LinguaFlow is a professional, local-first, multilingual language-learning web app. Use standalone Blazor WebAssembly, .NET 10, C#, MudBlazor 9.x, custom CSS/design tokens, static JSON courses, IndexedDB for learner data, browser Web Speech API for text-to-speech, .NET localization, and PWA/offline support. Deploy static assets to GitHub Pages. There is no backend API, database server, mandatory login, or paid AI API.

Default interface culture is `vi-VN`; also support `en-US`. Provide light/dark themes and responsive, accessible UI. Learning-language codes are `en`, `ja`, `zh`. English is active; Japanese and Chinese are coming soon. Directory names are deliberately `English`, `Japan`, and `China` (exact casing), independent of language codes.

The completed product needs multiple courses per language; catalog, dashboards and unit roadmaps; interactive grammar, reading, writing, listening and speaking practice; deterministic grading and rubric assessment; flashcards/spaced repetition; TTS; notes, bookmarks and mistake notebook; progress tracking; per-course progress export/import; course document downloads; Vietnamese/English UI; user guide, About page, PWA and GitHub Pages deployment. Show a capability as available only when it works.

## ChatGPT practice contract

Support normal ChatGPT conversations via a self-contained practice file the user downloads and attaches or pastes. The conversation follows the file's instructions and returns structured assessment JSON. The site imports and validates that JSON, computes its own rubric verdict, and persists the attempt locally. Never imply direct synchronization or require a ChatGPT Project, Custom GPT, OpenAI API, API key, or backend integration. Do not claim listening or pronunciation assessment from text alone.

## Content and privacy

Preserve original materials under `CourseSources/English`, `CourseSources/Japan`, and `CourseSources/China`; never move, rename, overwrite, or delete them. Keep source data separate from generated published content under `src/LinguaFlow.Web/wwwroot/Courses/English`, `Japan`, and `China`. Use exact path casing for GitHub Pages. Never fabricate educational content. Audit content and answer-key exposure before publishing. Do not publish learner progress, private notes, or other private user data as static assets. Keep course downloads distinct from user progress backups. Never silently reset progress; version local schemas and provide explicit migration/error handling.

LCF v1 is documented in `docs/COURSE_FORMAT.md`; the developer converter is in `tools/LinguaFlow.CourseConverter`. For the English source, prefer individual lesson and answer files, cross-check combined books and Curriculum, and keep ambiguous answers in manual review with a report warning. Source provenance in public JSON must use course-relative paths only. Static assessment JSON is inspectable by users even when the UI hides answers before submission.

For the future About page use only: Nguyễn Trần Anh Tân, born 2003, Software Engineer. Do not invent contact details, social profiles, employers, schools, photos, or biography.

## Engineering rules

Keep components small and maintainable; use strongly typed models, appropriate async/cancellation, clear errors, and minimal dependencies. Add meaningful automated tests where feasible, then run build and relevant tests. Document architectural decisions. Check browser limitations and offline behavior rather than assuming support. `LinguaFlow.Core` owns pure learning logic and contracts; `LinguaFlow.Web` owns browser UI/adapters; `LinguaFlow.CourseConverter` is a developer CLI; test projects cover domain, components, and browser flows. Follow `docs/ARCHITECTURE.md`, `docs/REPOSITORY_STRUCTURE.md`, and `docs/IMPLEMENTATION_PLAN.md` unless a later explicit request changes the plan.
