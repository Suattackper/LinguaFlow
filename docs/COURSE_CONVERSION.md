# Course conversion and source integrity

The original English curriculum is developer source in `CourseSources/English/english-life-office-it`. Do not edit it to change the published format. `LinguaFlow.CourseConverter` prefers the 36 individual lesson files and 36 individual answer files, cross-checks the combined books and Curriculum, then publishes LCF v1 under `src/LinguaFlow.Web/wwwroot/Courses/English/english-life-office-it`.

```powershell
dotnet run --project tools/LinguaFlow.CourseConverter -- convert CourseSources/English src/LinguaFlow.Web/wwwroot/Courses docs/COURSE_CONVERSION_REPORT.md
dotnet run --project tools/LinguaFlow.CourseConverter -- validate src/LinguaFlow.Web/wwwroot/Courses
node tools/deploy/audit-course.mjs
```

The generated report records 36/36 Units, 760 sections, 858 numbered exercises, 720 vocabulary entries, 72 rubric sections and 52 ambiguous or multipart answer mappings that remain for manual review. Only 36 multiple-choice items were identified as unambiguous automatic grading candidates. Open writing, speaking and listening work remains manually or externally assessed. The Step 13 audit checks every published Unit ID, title, objective presence, section provenance and unique block/exercise IDs against individual source lessons; it also samples Units 01, 12, 18, 30 and 36, including checkpoints and capstone. This structural audit does not certify the semantic correctness of all 858 answers. See [the detailed conversion report](COURSE_CONVERSION_REPORT.md) and [LCF specification](COURSE_FORMAT.md).

Published course downloads are an additional approval step after conversion. The two original approved PDFs and their ZIP are described in [Course Materials](COURSE_MATERIALS.md). `TRACKING/` is never a public learning resource.
