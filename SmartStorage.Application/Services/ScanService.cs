using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;
using SmartStorage.Application.DTOs;
using SmartStorage.Infrastructure.Storage;

namespace SmartStorage.Application.Services;

public sealed class ScanService
{
    private readonly IFileScanner _scanner;
    private readonly IMetadataExtractor _metadata;
    private readonly SqliteFileRepository _repository;

    public ScanService(IFileScanner scanner, IMetadataExtractor metadata, SqliteFileRepository repository)
    {
        _scanner = scanner;
        _metadata = metadata;
        _repository = repository;
    }

    public async Task<IReadOnlyList<FileRecord>> ScanAsync(string root, CancellationToken cancellationToken = default)
    {
        var items = new List<FileRecord>();
        await foreach (var record in _scanner.ScanAsync(root, cancellationToken))
        {
            try
            {
                var enriched = await _metadata.EnrichAsync(record, cancellationToken);
                // Persist to database index
                await _repository.UpsertAsync(enriched, cancellationToken);
                items.Add(enriched);
            }
            catch
            {
                // Keep scanning even if one file fails metadata enrichment.
                // Still persist the basic record
                await _repository.UpsertAsync(record, cancellationToken);
                items.Add(record);
            }
        }
        return items;
    }
}
