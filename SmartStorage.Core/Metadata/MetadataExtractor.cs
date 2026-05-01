using System.Security.Cryptography;
using System.IO.Hashing;
using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Core.Metadata;

public enum HashAlgorithm
{
    None,
    XXHash64,
    SHA256
}

public sealed class MetadataExtractor : IMetadataExtractor
{
    private readonly HashAlgorithm _hashAlgorithm;
    private readonly long _maxHashFileSizeBytes;

    public MetadataExtractor(HashAlgorithm hashAlgorithm = HashAlgorithm.XXHash64, long maxHashFileSizeBytes = 25 * 1024 * 1024)
    {
        _hashAlgorithm = hashAlgorithm;
        _maxHashFileSizeBytes = maxHashFileSizeBytes;
    }

    public async Task<FileRecord> EnrichAsync(FileRecord record, CancellationToken cancellationToken = default)
    {
        if (record.IsDirectory || !File.Exists(record.Path))
        {
            return record with { Tags = Array.Empty<string>() };
        }

        string? hash = null;
        if (_hashAlgorithm != HashAlgorithm.None && record.SizeBytes <= _maxHashFileSizeBytes)
        {
            try
            {
                hash = await ComputeHashAsync(record.Path, cancellationToken);
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

    private async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);

        return _hashAlgorithm switch
        {
            HashAlgorithm.XXHash64 => await ComputeXXHash64Async(stream, cancellationToken),
            HashAlgorithm.SHA256 => await ComputeSHA256Async(stream, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported hash algorithm: {_hashAlgorithm}")
        };
    }

    private async Task<string> ComputeXXHash64Async(Stream stream, CancellationToken cancellationToken)
    {
        var xxHash = new XxHash64();
        var buffer = new byte[81920]; // 80KB buffer for optimal performance

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            xxHash.Append(buffer.AsSpan(0, bytesRead));
        }

        return xxHash.GetCurrentHash().ToHexString();
    }

    private async Task<string> ComputeSHA256Async(Stream stream, CancellationToken cancellationToken)
    {
        var bytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(bytes);
    }
}

internal static class ByteArrayExtensions
{
    public static string ToHexString(this byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
