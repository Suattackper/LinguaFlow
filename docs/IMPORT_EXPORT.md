# Course progress backup and restore

Open a course, choose **Progress backup**, then export its JSON backup. The file contains local progress, attempts, reviews, notes, bookmarks and practice results for that course; it does not contain the curriculum. Keep it privately because notes and drafts can be personal. To transfer devices, copy the file to the second device and import it into the same course. There is no automatic cloud sync.

Import validates the schema, course ID, content version/fingerprint and referenced IDs before showing a preview. Confirming replaces that course's stored learning records in a transaction, with a recoverable pre-import snapshot where supported. A wrong-course, malformed or incompatible file is rejected without replacing data. Repeated imports use Replace semantics; the preview states this clearly. Export a fresh backup before resetting a course or clearing browser data.

Course materials such as Student Book and Answer Key PDFs are separate downloads and are not included in progress backups. See [storage and backup implementation](LOCAL_STORAGE_AND_BACKUPS.md) and the in-app Help page for step-by-step instructions.
