# PWA, offline courses, and GitHub Pages

LinguaFlow remains a standalone .NET 10 Blazor WebAssembly app. Release publishing generates a versioned service-worker asset manifest. The published worker caches the application shell, course catalog and Blazor runtime, including CSS, JavaScript, fonts supplied as static assets, icons and localization data. It excludes the rest of the `Courses/` tree from automatic shell pre-caching, so opening Home does not download all 36 Units or public PDF/ZIP downloads. The development worker performs no caching.

## Install and study offline

- Android Chrome: open the HTTPS site and use the browser's **Install app** menu when available.
- iPhone Safari: use **Share → Add to Home Screen**. Install behavior and offline capabilities vary by browser and device.
- Open a course and choose **Offline learning → Download course** while connected. Wait until the actual cached Unit count reaches the manifest's Unit count. This saves Unit JSON, vocabulary, assessments, practice configuration, and declared media assets. PDF/ZIP documents remain separate downloads.
- IndexedDB stores answers, progress, bookmarks and notes separately from CacheStorage. The offline manager never resets IndexedDB. Browser storage can be evicted, so export a course progress backup and keep it safely.
- Cached lessons, local grading, vocabulary review, notes, dashboards and backup export can work offline. ChatGPT conversations and result generation need Internet. Browser speech synthesis depends on installed voices and may not work offline. A browser online/offline event is only a hint, not a network reachability test.

The offline manager fetches a fresh manifest through the network, downloads its files, checks course IDs and the deterministic SHA-256 fingerprint, then switches to the complete new cache only after success. If the fingerprint changed, the user sees a warning. Updates are refused if Unit, section, block, exercise or vocabulary IDs change, or if exercise content, assessment rules or completion rules change; no migration exists for those changes. Existing offline content and IndexedDB records are retained. The app shell worker updates separately and keeps the old shell until the new one installs successfully and all tabs using the old worker close. Browser CacheStorage is best effort; private backups are downloaded as Blob URLs and are never stored as public course assets.

## GitHub Pages

The workflow `.github/workflows/deploy.yml` builds and tests the solution, validates public LCF files, publishes the WebAssembly app, prepares GitHub Pages routing, validates the published tree, runs a headless Chrome offline smoke test, and uploads the `wwwroot` artifact. The `deploy` job uses GitHub's Pages deployment action. Configure **Settings → Pages → Build and deployment → GitHub Actions** in the repository. Publishing occurs on `main` or `master` pushes or manual workflow dispatch. No GitHub username is hardcoded, and the workflow does not commit or push.

The base path is generated from the repository name (`/${{ github.event.repository.name }}/`). For a custom project path, run:

```powershell
dotnet restore LinguaFlow.slnx
dotnet build LinguaFlow.slnx -c Release --no-restore
dotnet test LinguaFlow.slnx -c Release --no-build --no-restore
node tools/deploy/set-base.mjs src/LinguaFlow.Web/wwwroot/index.html /LinguaFlow/
dotnet publish src/LinguaFlow.Web/LinguaFlow.Web.csproj -c Release --no-restore -o artifacts/publish
node tools/deploy/prepare-pages.mjs artifacts/publish/wwwroot /LinguaFlow/
dotnet run --project tools/LinguaFlow.CourseConverter -c Release --no-build -- validate-publish artifacts/publish/wwwroot /LinguaFlow/
node tools/deploy/smoke-publish.mjs artifacts/publish/wwwroot /LinguaFlow/
```

Restore the source `index.html` base href to `/` after local publish testing; the CI checkout is disposable. The base tag is changed **before** publish. The preparation script resolves any runtime script/import-map placeholders left by the SDK, updates the service-worker integrity hash for the final `index.html`, and removes stale compressed copies of changed files. `404.html` redirects deep links through a `?p=` query, and `index.html` restores browser history before Blazor starts. `.nojekyll` preserves `_framework/`. Static course URLs and download URLs resolve relative to the app base and manifest URL. GitHub Pages serves static files only; no server rewrite or backend is required.

The deployment validator checks the PWA files, base path, SPA fallback, manifest, published LCF references and fingerprint, declared downloads, and forbidden private/developer file patterns. The GitHub Pages artifact contains only `publish/wwwroot`; it does not contain `CourseSources`, docs, tests, or local progress. The public site is live only after a successful GitHub Actions deployment.
