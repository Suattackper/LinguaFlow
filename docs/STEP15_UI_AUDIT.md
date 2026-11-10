# STEP 15 baseline audit

Date: 2026-11-10. Source is the existing Step 14 Blazor WebAssembly app. Baseline Release build: 0 errors, 0 warnings. Baseline tests: 85 passed (52 Core, 21 Web, 12 converter). Chrome CDP screenshots are under `artifacts/visual/step15-before/`.

## Inventory

`MainLayout` owns desktop sidebar, mobile drawer, top bar, breadcrumbs and footer. `NavMenu` supplies navigation. `Settings` uses four native controls and reset actions. The remaining Razor pages cover home, language catalog, course dashboard/roadmap, Unit reader, vocabulary/flashcards/review, practice/import/result, progress, backup, materials/offline, goals/workspace/mistakes, Help, About, Privacy and missing routes. `app.css` is the original stylesheet; Step 14 `redesign.css` is loaded last. MudBlazor supplies theme, dialogs, snackbars and popovers. JavaScript adapters own preferences, motion and the mobile drawer.

## Prioritized defects

1. Native selects have inconsistent browser arrows and insufficient trailing space, especially in Settings and the mobile toolbar. Native open menus are browser-owned and need explicit visual/keyboard checks.
2. The desktop sidebar cannot collapse. It reserves 252px at all desktop widths and has no stored preference.
3. Settings is a single dense card. The Help arrow sits separately above it; checkboxes are small and lack switch treatment; reset actions are crowded.
4. Some cards/actions inherit older spacing from `app.css`; page-wide shared rules need a consistent rhythm.
5. The first-load state is plain text on an empty page. It needs recognizable branding and purposeful animation with reduced-motion fallback.
6. Step 14 animations cover a few elements, but selection, progress and route feedback need more consistent restrained treatment.

No course JSON, grading, persistence records or routes require changes for these visual fixes. Playwright is not installed locally; Chrome DevTools Protocol automation is available and was used for the baseline screenshots. The 390/768/1024/1440/1692px, light/dark, zoom and open-dropdown matrix remains part of final verification.
