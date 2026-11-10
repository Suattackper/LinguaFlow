# Static deployment

The repository contains `.github/workflows/deploy.yml` for GitHub Pages. Configure the repository's Pages source to **GitHub Actions**, then run the workflow or push to its configured branch. The workflow restores, builds, tests, publishes, prepares the project base path, validates public assets, runs production Chrome smoke coverage, and deploys `wwwroot` only. No GitHub username is embedded.

For local release verification, follow [PWA and deployment commands](PWA_AND_DEPLOYMENT.md). The publish validator checks LCF paths and fingerprints, downloads, SPA fallback and forbidden private files. The base href must be set before publish; the source file must be restored after a local test. `404.html` supports deep links on GitHub Pages. A workflow file does not mean a public deployment succeeded; verify the Actions run and public URL separately.
