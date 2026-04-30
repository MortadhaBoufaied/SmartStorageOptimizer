using System.Security.Cryptography;
using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Core.Metadata;

public sealed class MetadataExtractor : IMetadataExtractor
{
    public async Task<FileRecord> EnrichAsync(FileRecord record, CancellationToken cancellationToken = default)
    {
        if (record.IsDirectory || !File.Exists(record.Path))
        {
            return record with { Tags = Array.Empty<string>() };
        }

        string? hash = null;
        if (record.SizeBytes < 25 * 1024 * 1024)
        {
            try
            {
                await using var stream = File.OpenRead(record.Path);
                var bytes = await SHA256.HashDataAsync(stream, cancellationToken);
                hash = Convert.ToHexString(bytes);
            }
            catch (IOException)
            {
                // Locked files are expected on Windows user profiles; continue without hash.
            }
            catch (UnauthorizedAccessException)
            {
                // Some system files are inaccessible; continue without hash.
            }
        }

        var tags = new List<string>();
        if (record.Extension is ".tmp" or ".cache" or ".bak") tags.Add("temporary");
        if (record.SizeBytes > 1_000_000_000) tags.Add("large");
        if (record.Extension is ".zip" or ".7z" or ".rar") tags.Add("archive");
        if (record.Extension is ".docx" or ".xlsx" or ".pptx" or ".pdf" or ".md" or ".txt") tags.Add("document");

        return record with { Hash = hash, Tags = tags };
    }
}
