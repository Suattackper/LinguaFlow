using System.Text.Json;
using System.Text.Json.Serialization;

namespace LinguaFlow.Core.Courses;

public static class CourseJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        Converters = { new JsonStringEnumConverter<CourseStatus>(JsonNamingPolicy.KebabCaseLower) }
    };
}
