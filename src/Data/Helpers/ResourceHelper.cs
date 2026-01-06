using System.Text.Json;

namespace MRVA.Reports.Data.Helpers;

internal static class ResourceHelper
{

    internal static T? GetResource<T>(string path)
    {
        var assembly = typeof(ResourceHelper).Assembly;

        var fileStream = assembly
            .GetManifestResourceStream($"{Constants.Namespace}.Resources.{path}");

        if (fileStream == null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(fileStream) ?? default;
    }

}