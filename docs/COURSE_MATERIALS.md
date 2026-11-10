# Course materials publishing

The approved public resources for `english-life-office-it` are the existing `StudentBook_Units01-36.pdf` and `AnswerKey_Units01-36.pdf` from the original course `PDF/` folder. The converter copies them byte for byte into `Downloads/` and creates `EnglishForLifeOfficeIT_CompleteCourse.zip` containing exactly those two PDFs. The package is complete with respect to its stated contents, not a copy of every developer source file. No Markdown conversion or new PDF generation is needed because original PDFs exist.

From the repository root:

```powershell
dotnet run --project tools/LinguaFlow.CourseConverter -- publish-materials CourseSources/English src/LinguaFlow.Web/wwwroot/Courses
dotnet run --project tools/LinguaFlow.CourseConverter -- validate src/LinguaFlow.Web/wwwroot/Courses
```

`convert` and `rebuild` run the same materials publisher after curriculum generation. Publishing fails if an approved source PDF is absent or lacks a PDF signature. Validation checks each declared download's path, presence, size and basic format; tests compare ZIP members byte for byte with source PDFs. Publication never copies `TRACKING/`, teacher instructions, or personal progress. The Answer Key is public static content and must never be presented as access controlled. Backups are separate personal JSON files created in the browser.

The materials page loads only the catalog and manifest, then checks declared download URLs with `HEAD` and hides missing files or SPA HTML fallbacks. Links use `CoursePathResolver.ResolveContent` from the manifest URL, so deployments under a GitHub Pages project path work. Offline downloads require those files to have been cached; the page does not promise pre-caching. Hosts that block `HEAD` will hide downloads until that hosting behavior is addressed.
