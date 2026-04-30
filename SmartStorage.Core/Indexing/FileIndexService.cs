using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;
using SmartStorage.Infrastructure.Storage;

namespace SmartStorage.Core.Indexing;

public sealed class FileIndexService
{
    private readonly FileRepository _repository;
    private readonly IFileScanner _scanner;
    private readonly IMetadataExtractor _metadata;
    private readonly IServiceProvider _serviceProvider;
    private readonly HashSet<string> _excludedPaths;

    private static readonly string[] DefaultExcludes = new[]
    {
        "C:\\Windows",
        "C:\\Program Files",
        "C:\\Program Files (x86)",
        "C:\\ProgramData",
        "C:\\$Recycle.Bin",
        "C:\\System Volume Information",
        "C:\\Recovery",
        "C:\\PerfLogs",
        // Note: intentionally NOT excluding Downloads by default
    };

    public FileIndexService(FileRepository repository, IFileScanner scanner, IMetadataExtractor metadata, IServiceProvider serviceProvider, IEnumerable<string>? excludedPaths = null)
    {
        _repository = repository;
        _scanner = scanner;
        _metadata = metadata;
        _serviceProvider = serviceProvider;

        _excludedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in DefaultExcludes) _excludedPaths.Add(Path.GetFullPath(d));
        if (excludedPaths != null)
        {
            foreach (var p in excludedPaths)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;
                // normalize and add, but skip any path that ends with "Downloads" to preserve indexing there
                try
                {
                    var full = Path.GetFullPath(p);
                    if (full.TrimEnd(Path.DirectorySeparatorChar).EndsWith("Downloads", StringComparison.OrdinalIgnoreCase))
                        continue;
                    _excludedPaths.Add(full);
                }
                catch
                {
                    // ignore invalid paths from config
                }
            }
        }
    }

    public bool IsPathAllowed(string path)
    {
        var normalized = Path.GetFullPath(path);
        foreach (var ex in _excludedPaths)
        {
            if (normalized.StartsWith(ex, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return true;
    }

    public async Task<int> IndexPathAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        if (!IsPathAllowed(rootPath))
        {
            throw new InvalidOperationException($"Path is not allowed for indexing: {rootPath}");
        }

        var count = 0;
        await foreach (var record in _scanner.ScanAsync(rootPath, cancellationToken))
        {
            try
            {
                var enriched = await _metadata.EnrichAsync(record, cancellationToken);
                await _repository.UpsertAsync(enriched, cancellationToken);
                count++;
            }
            catch
            {
                // Continue indexing on metadata enrichment failure
                await _repository.UpsertAsync(record, cancellationToken);
                count++;
            }
        }
        return count;
    }

    public async Task<IReadOnlyList<FileRecord>> GetIndexedFilesAsync(string? rootPath = null, CancellationToken cancellationToken = default)
    {
        return await _repository.GetIndexedFilesAsync(rootPath, cancellationToken);
    }

    public async Task<int> DeleteStaleEntriesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        return await _repository.DeleteStaleEntriesAsync(maxAge, cancellationToken);
    }
}
