using System.IO.Compression;

namespace ClientUploader;

public static class Zipper
{
    public static string ZipFolder(string folderPath, string? outputDir = null)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException(folderPath);

        outputDir ??= Path.GetTempPath();
        Directory.CreateDirectory(outputDir);

        var fileName = $"upload_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
        var zipPath = Path.Combine(outputDir, fileName);

        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(folderPath, zipPath, CompressionLevel.Fastest, true);
        return zipPath;
    }
}