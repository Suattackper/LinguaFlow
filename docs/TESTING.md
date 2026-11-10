# Testing and release verification

```powershell
dotnet restore LinguaFlow.slnx
dotnet build LinguaFlow.slnx -c Release --no-restore
dotnet test LinguaFlow.slnx -c Release --no-build --no-restore
node tools/deploy/audit-course.mjs
```

The .NET suites cover course format, conversion, grading, backup validation, ChatGPT protocol/import rules, vocabulary, analytics, components and published materials. `LinguaFlow.E2E` currently contains no .NET test cases; browser automation is in `tools/deploy/smoke-publish.mjs`. Run it against a prepared **Release publish**, not a debug development server, using the procedure in [PWA and deployment](PWA_AND_DEPLOYMENT.md).

The browser smoke checks the real service worker, manifest, subpath routes, UI language switching, responsive widths, declared downloads, a Unit 01 Speaking package export and matching deterministic assessment JSON preview/import/persistence, all 36 cached Units, compatible and incompatible content updates, offline lesson loading, offline exercise submission to IndexedDB and progress preservation after course-cache removal. It uses a fresh temporary Chrome profile. A live ChatGPT conversation, actual PWA installation and physical mobile device behavior require manual verification; do not infer them from headless Chrome.
