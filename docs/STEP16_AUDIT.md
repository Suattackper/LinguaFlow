# Step 16 audit

## Baseline

- Source: `main` at `f50f9a8`; Release build passed with no warnings or errors before changes.
- Standalone Blazor WebAssembly on .NET 10. The published service worker precaches application JS/CSS from the asset manifest and keeps course caches separate from IndexedDB.
- UI preferences store `vi-VN` or `en-US`, theme, reduced motion, large text and sidebar state. Course language routes (`en`, `ja`, `zh`) are distinct from UI culture. Language selection in the top bar is currently a wide native select.
- Existing `motion.js` uses the browser Web Animations API and IntersectionObserver. Step 15 CSS also animates lesson sections, roadmap cards and practice panels. Replacing the entrance implementation must remove the overlapping CSS animation.
- Unit reader already has a reading Focus Mode and keeps notes/drafts in the existing workspace repository. The redesign must keep these controls and not alter published course JSON or IndexedDB schemas.
- Main routes include home, language pages, course dashboard/roadmap/units/progress/practice/materials/offline/backup, vocabulary and review, settings, help, about and privacy.

## Risks to verify

- First-load failure must show the existing 15-second recovery control rather than an endless splash.
- GitHub Pages runs under `/LinguaFlow/`; dynamic imports and route links must use the document base URL.
- The roadmap currently loads each Unit for objective/activity previews; avoid adding another eager Unit fetch in the command palette.
- Browser motion must stop for operating-system reduced motion and the app preference.
- Validate desktop/mobile layouts, keyboard focus, theme and culture after the new controls are wired.
