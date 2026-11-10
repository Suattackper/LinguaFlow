# Local learning data

LinguaFlow stores learner records in versioned browser IndexedDB. It stores interface bootstrap preferences in localStorage and downloaded public lesson content in CacheStorage. Learner data is scoped by course ID; clearing offline course content does not clear progress. The application has no required account or backend database.

Browser storage can be cleared by the user or evicted by the browser, especially under storage pressure. Export progress backups regularly. A backup file may contain personal notes, writing drafts and AI feedback. Reset actions require confirmation. [Implementation and migration details](LOCAL_STORAGE_AND_BACKUPS.md) document the record schema, transaction behavior and recovery snapshots.

Opening the site alone does not count as a completed study session. Dashboard figures are calculated from stored activity and remain empty for new learners.
