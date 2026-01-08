using MRVA.Reports.Data.Extensions;

namespace MRVA.Reports.Data.Helpers;

internal static class ResourceHelper
{

    internal static Span<byte> GetResource(string path)
    {
        return typeof(ResourceHelper)
            .Assembly
            .GetManifestResourceStream($"{Constants.Namespace}.Resources.{path}")?
            .ReadAllBytes() ?? Span<byte>.Empty;
    }
    
}