# Step 06 assessment engine

## Original source policy

The published English course has 858 numbered questions. The Step 03 source audit marked 36 single-choice questions as `auto-choice`; the other 822 answer mappings are `manual-review`. The reader now fetches the assessment JSON when a Unit opens but does not show answers before submission. The file remains a public static asset, so Test mode is a learning mode, not a secure examination.

`ExerciseDefinitionFactory` maps LCF block and response kinds to typed exercises. It enables automatic grading only for unambiguous `auto-choice` keys or a future explicitly authored `auto-exact` key. `validAnswers`, `allowPartialCredit`, and `ignorePunctuation` are optional LCF answer metadata; the existing converter does not infer alternatives from punctuation such as `/` or `;`. No original question is replaced with generated content. Only types represented by authored content are shown.

## Grading and feedback

Core strategies cover choice, set/matching/multiple blanks, ordering, exact text, and manual review. Comparison normalizes Unicode to NFC, trims and collapses whitespace, and uses case-insensitive matching by default. Punctuation is ignored only when the answer metadata explicitly permits it. Multiple-select requires the exact set. Ordered answers keep their order. Partial credit is available only when explicitly permitted. Empty submissions and unknown choices are rejected. Open writing/speaking are submitted for rubric review without an invented score. Listening text with no audio evidence is marked insufficient evidence.

The UI supports Practice (feedback after explicit submission), Test (collect answers, hide previous results until submission), and Review (focus on unreviewed mistakes). Answers are saved as drafts in IndexedDB. Every submission creates a timestamped `ExerciseAttempt` record; incorrect or partially correct graded submissions update one `Mistake` record per exercise. The mistake notebook records the learner answer, original answer, explanation, skill and review status. Attempts and mistakes are included in per-course progress backups; the optional `mistakes` array remains backward-compatible with Step 05 backup v1.

## Completion

`UnitCompletionService` reads each Unit's original threshold lines from `Assessments/assessments.json`. Numeric thresholds are evaluated only when enough graded evidence exists. Manual-review answers cannot silently count as passing scores. Writing, speaking, listening and source progression lines remain visible as unmet requirements until supported evidence exists. Viewing a Unit or filling a draft never marks it complete. The current course's open assessment rules mean Unit completion can remain `NeedsReview` until later manual or external assessment support is added. This is intentional and avoids a false completion claim.

## Verification

Run `dotnet test LinguaFlow.slnx` and `dotnet build LinguaFlow.slnx`. In a browser, open Unit 01, submit its first C2 multiple-choice question, review feedback and explanation, refresh to confirm history, then open the mistake notebook. Switch to Test before submitting to confirm prior solutions are hidden. Check writing submission for a rubric-based `NeedsReview` state and listening text for `InsufficientEvidence`.
