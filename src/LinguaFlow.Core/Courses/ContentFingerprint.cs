using System.Security.Cryptography;
using System.Text;

namespace LinguaFlow.Core.Courses;

public static class ContentFingerprint
{
    public static string Compute(IEnumerable<(string Path, byte[] Bytes)> files)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var (path, bytes) in files.OrderBy(x => x.Path, StringComparer.Ordinal))
        {
            if (!CoursePathResolver.IsSafeRelativePath(path)) throw new CourseContentException("Invalid fingerprint input path.");
            hash.AppendData(Encoding.UTF8.GetBytes(path));
            hash.AppendData([0]);
            hash.AppendData(Encoding.UTF8.GetBytes(bytes.LongLength.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            hash.AppendData([0]);
            hash.AppendData(bytes);
        }
        return "sha256:" + Convert.ToHexStringLower(hash.GetHashAndReset());
    }
}
