namespace LinguaFlow.Core.Courses;

/// <summary>Explicit migrations for published content changes that preserve all stable learner IDs.</summary>
public static class CourseContentCompatibility
{
    private const string PreviousEnglishFingerprint = "sha256:0818b5eb0a159e10d19257a1b1af01849ee02055215fbda78a9757522807261c";
    private const string CurrentEnglishFingerprint = "sha256:a5bf7267189c280419cdfdc4459e1e36d6eeb87009292faf923310ba6211d517";

    public static bool Accepts(CourseManifest manifest, string version, string fingerprint) =>
        manifest.ContentVersion == version && (manifest.ContentFingerprint == fingerprint ||
            manifest.CourseId == "english-life-office-it" && version == "1.1.0" &&
            manifest.ContentFingerprint == CurrentEnglishFingerprint && fingerprint == PreviousEnglishFingerprint);
}
