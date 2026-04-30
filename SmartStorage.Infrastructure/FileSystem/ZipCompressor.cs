using System.IO.Compression;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Infrastructure.FileSystem;

public sealed class ZipCompressor : ICompressor
{
    public string Name => "Zip";

    public Task<string> CompressAsync(string sourcePath, string destinationArchivePath, CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(sourcePath))
        {
            if (File.Exists(destinationArchivePath)) File.Delete(destinationArchivePath);
            ZipFile.CreateFromDirectory(sourcePath, destinationArchivePath, CompressionLevel.Optimal, includeBaseDirectory: true);
        }
        else if (File.Exists(sourcePath))
        {
            using var zip = ZipFile.Open(destinationArchivePath, ZipArchiveMode.Create);
            zip.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath), CompressionLevel.Optimal);
        }
        else
        {
            throw new FileNotFoundException("Path not found", sourcePath);
        }
        return Task.FromResult(destinationArchivePath);
    }
}
