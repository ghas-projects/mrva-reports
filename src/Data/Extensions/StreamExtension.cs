namespace MRVA.Reports.Data.Extensions;

public static class StreamExtension
{
    public static byte[] ReadAllBytes(this Stream inStream)
    {
        if (inStream is MemoryStream inMemoryStream)
        {
            return inMemoryStream.ToArray();
        }

        using var outStream = new MemoryStream(); 
        inStream.CopyTo(outStream);
        return outStream.ToArray();
    }
}