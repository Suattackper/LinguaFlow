# Local learning storage and course backups (Step 05)

## Browser database

`wwwroot/js/learning-store.js` owns all IndexedDB calls. Database `linguaflow-learning` is at version 2. The v1 `records` store is retained on upgrade; v2 adds `snapshots` and `metadata`. Opening a newer or blocked database reports an error and never resets it. A `records.courseId` index isolates course queries. The storage key is `courseId|kind|id`; `put` deliberately replaces the same key, so duplicate writes are deterministic. All record payloads have `recordSchemaVersion: 1` and an update timestamp.

`LearningRepository` exposes entity repositories for course, unit and section progress; attempts; drafts; bookmarks; notes; vocabulary and review states; sessions; goals; AI practice and assessment; course settings; and user preferences. It also exposes atomic batch writes, course replacement, delete and reset operations. Course-specific entities always have `courseId`. The global UI preferences use `courseId: global` in this repository and retain their existing localStorage bootstrap copy so culture/theme can be applied before the app runs.

The reader hydrates `IReaderWorkspace` from IndexedDB on course entry. Position and bookmarks are saved at interaction boundaries. Long text edits use a 350 ms debounce and flush on change, section navigation and export. Storage failures show a warning; the in-memory edit remains available until the page closes. Browser storage may be blocked, cleared or quota-limited. The app never silently clears a database on schema or read errors.

## Per-course backup v1

The JSON document uses `format: linguaflow-course-progress`, `backupSchemaVersion: 1`, `courseId`, `contentVersion`, `contentFingerprint`, `exportedAt`, and arrays named `progress`, `attempts`, `reviewStates`, `notes`, `bookmarks`, `studySessions`, `aiPracticeResults`, `courseSettings`, `drafts`, `vocabularyStates`, and `learningGoals`. Step 06 adds optional `mistakes`; older version 1 backups remain valid. It contains learner records only, not curriculum JSON, static answer keys or public downloads. The exported filename is `linguaflow-<course-id>-progress-YYYY-MM-DD.json`. The export page reports the record count and last export time. Backups can include private notes and answers.

Import accepts at most 5 MB, parses JSON, checks format and schema, exact course ID, exact content version and fingerprint, record kind/group, duplicate keys, ID syntax and references to the current manifest/units. No cross-version migration is defined in v1; incompatible backups are rejected without writes. The preview shows incoming and existing record counts. Confirmed Replace creates a snapshot of the prior course records, deletes that course's records and writes incoming records **in one IndexedDB transaction**. Any request error or interruption aborts the transaction. The snapshot can be restored through the same atomic replacement path. Reimporting the same file yields the same learner records. Reset one course also snapshots first; Reset All removes all learning records and recovery snapshots after a typed `DELETE` confirmation.

## Manual checks

1. Open a Unit, move to another section, type a note, and refresh. Check that the position and note return.
2. Export from `/courses/english-life-office-it/backup`; inspect the JSON header and record count, then import it into another browser profile with the same published course version.
3. Try malformed JSON, a different course ID and a changed fingerprint. The preview must reject each without changing existing records.
4. Import a valid file, verify the restored position, then use the recovery snapshot to return to the previous state.
5. In browser settings with storage blocked, verify that the UI warns and leaves the course readable.

Storage quota estimates are shown only when `navigator.storage.estimate()` is supported. Backups are manual; the app does not schedule notifications or cloud sync. The optional all-courses backup is not implemented in Step 05.
