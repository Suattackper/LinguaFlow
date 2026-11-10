# Manual ChatGPT assessment import (Step 09)

The Practice Center, its session history, and Unit Writing/Speaking blocks link to `/courses/{courseId}/practice/import`. The learner may upload a UTF-8 `.json` file of at most 64 KiB or paste one JSON object, including a single ChatGPT fenced JSON block. The page validates and previews without writing. Cancel discards the preview. Confirm revalidates and writes the result and updated session in one IndexedDB batch transaction.

## Validation and matching

`PracticeResultValidator` in Core implements the version 1 contract from `CHATGPT_PRACTICE_PROTOCOL.md`. It requires the exact format and schema, external AI evaluator marker, bounded text fields, supported modality and evidence levels, plausible completion date, and one unique score for every exported criterion. Each criterion ID and maximum points come from the saved request snapshot; unknown or impossible scores fail. Audio-required criteria cannot receive points from text-only evidence. The current course content version and fingerprint must still match the saved session. The original `requestId` must exist in course-scoped IndexedDB. If it does not, the UI asks for the course backup containing the original session. No result is attached by guessing a Unit.

`PracticeResultService` performs the course and task lookup, duplicate detection, preview and transaction. A deterministic result ID hashes the parsed canonical result; reimporting the same result does not create another attempt. Distinct results remain in history. The imported record stores the original payload, computed evaluation, import time, and references. The existing per-course backup includes both `AiPracticeSession` and `AiAssessmentResult`; backup validation checks their references and linked request IDs. Restore still uses the Step 05 Replace transaction and recovery snapshot.

## Verdict boundaries

The website calculates the rubric score from the exported criteria. It uses a pass threshold only when the actual course assessment declares one for that Unit and skill. The current English Speaking and Writing rubrics specify criterion weights but no numeric pass threshold, so imported results show their score with `NotAssessed` for the pass decision; this does not erase the feedback. Missing required evidence yields `InsufficientEvidence`. A future course with an explicit skill threshold can produce `Passed` or `NeedsImprovement`. External AI text is never proof that a conversation occurred or that feedback is accurate. Pronunciation without suitable observed audio remains unverified; text-only listening cannot satisfy an audio-required criterion.

Unit progress reads imported results separately from exercise attempts. It does not rewrite unrelated exercises or scores. If an authored pass rule exists, any valid passing attempt can satisfy that skill even when a later attempt is weaker; all attempts remain in history. The result page links only to course content present in the Unit and offers a practice retry. Razor renders imported feedback as escaped text, never as HTML.

## Manual checks

In a browser, export a Speaking request, import its matching JSON, inspect the preview, cancel once, confirm, refresh, then export and restore a course progress backup. Also test a wrong request ID, an oversized file, a repeated import, and text-only pronunciation claims. Browser file permissions and IndexedDB availability can vary by device.
