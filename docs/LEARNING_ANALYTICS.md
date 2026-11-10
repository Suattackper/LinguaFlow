# Local learning analytics (Step 10)

Dashboards read only course-scoped IndexedDB records plus the static catalog, manifest, and one vocabulary reference. They do not fetch every Unit to calculate progress. The home page shows onboarding when no course has stored learning activity. Each course page and `/courses/{courseId}/progress` derives its values from saved position, Unit assessment, exercise attempts, vocabulary reviews, practice results, activity events, and backup metadata. Optional malformed records are skipped where they can be isolated; unavailable storage is reported.

## Metric definitions

- **Completed Units / course percentage:** saved `UnitAssessment.Status == Completed` divided by manifest Unit count. Opening a Unit or section does not complete it.
- **Exercise accuracy:** correct automatic outcomes divided by correct, incorrect, and partial graded attempts. Open assignments without deterministic grades are excluded. Grammar and reading use the same rule, restricted to those exercise IDs.
- **Vocabulary mastery:** words with saved `Known` state divided by the published vocabulary count. Due reviews come from saved due instants or an explicit review queue.
- **Last study / recent skill:** latest saved lesson position, study activity, attempt, vocabulary review, or practice import. The recently practiced skill comes from saved attempts or imported results.
- **Study frequency and streak:** local calendar days with stored learning events. A streak may begin today or yesterday, so the UI does not shame a missed day. Weekly days use Monday as the start of week.
- **Study minutes:** only an explicitly finished lesson session counts. The browser timer accumulates focused, visible time, pauses while hidden or unfocused, and caps a session at 120 minutes. Less than one active minute is not recorded. Displayed durations are estimates.
- **Goals:** daily minutes, weekly days, preferred session length, vocabulary review target, preferred time, and target course are stored as a course-scoped `LearningGoal` record. Goal progress never creates an activity event.
- **Achievements:** first Unit completed, first seven persisted vocabulary review events, and first imported Speaking result. They are derived from stored events and are never prefilled for a new learner.

## Recommendations and reminders

Core generates deterministic suggestions from due vocabulary, current Unit, saved mistakes, awaiting ChatGPT results, pending Unit requirements, and practice verdicts. Each suggestion points to an existing route and explains its reason. Suggested minutes are capped by the learner's daily goal and do not count as study activity. Skipping a card only hides it in the current view; refreshing recomputes the plan. A backup reminder appears after recorded study when no backup exists or the last one is over 14 days old. Reminders are calculated when the app is opened; none are delivered after the browser closes.

The personal workspace lists course-scoped notes, bookmarks, saved examples, writing drafts, and favorite vocabulary. The mistake notebook filters by Unit and skill, shows repeated incorrect attempts, and stores optional notes separately from attempt history. All these records remain private in IndexedDB and are included in the existing course progress backup. The course page links to actual manifest downloads only when they exist.

## Limits

The published English course currently has no independently completed Units in a fresh profile, and open-skill rubrics lack numeric pass thresholds. Dashboards therefore leave those statuses pending until supported evidence and course rules produce completion. Course-specific charts use simple HTML `progress` and CSS, without a chart package. No server, notification permission, or background timer is required.
