# Vocabulary review and speech (Step 07)

The English course publishes 720 vocabulary entries derived from `CourseSources/English` through the existing converter. The vocabulary library loads the manifest's vocabulary reference; it does not construct words from UI code. Optional part of speech, example, explanation, topic, and audio fields render only when the source provides them. Current published entries have no supplied audio files, so playback uses browser speech when a matching installed voice exists.

## Routes

- `/courses/{courseId}/vocabulary`: search, filter, group, sort, paginate, favorite, note, queue, and inspect review history.
- `/courses/{courseId}/review`: due, overdue, new, difficult, and recent counts with links into review sessions.
- `/courses/{courseId}/flashcards?mode=due|new|weak|favorite|all`: reveal, remembered/forgotten, keyboard shortcuts, and a saved session summary.

All routes resolve the course through the catalog and manifest. A vocabulary entry's course, Unit, and ID are checked before writing its review state. Review records and session summaries use the Step 05 IndexedDB repositories, so they are included in the existing per-course backup and Replace import transaction. Imported typed review states are checked for consistent counts and IDs before any writes.

## Scheduling

The default intervals are 1, 3, 7, and 14 days. An explicit Unit review schedule takes precedence. A remembered card advances one stage; a forgotten card moves one stage earlier and is due immediately. Instants are stored as UTC `DateTimeOffset` values. Dashboard calendar buckets use the viewer's local date; the due queue compares instants. No browser timers are scheduled for reviews. Opening a review screen rebuilds its queue from persisted states.

## Speech and listening

`speech.js` owns browser Web Speech listeners and cancellation. `TextToSpeechService` owns interop and the saved voice and rate preferences. Course supplied audio is preferred where present; unavailable media falls back to speech. Voice availability depends on the browser and operating system, and Web Speech is not guaranteed to work offline. Recording a playback event is evidence of playback only; it does not verify comprehension or pronunciation. Hidden listening transcripts are withheld from the rendered page until revealed after submission.

The original published vocabulary currently has no authored IPA, part of speech, examples, explanations, topics, or audio paths beyond fields actually found in source. These fields are optional rather than synthesized. The source's review schedules are kept where present.
