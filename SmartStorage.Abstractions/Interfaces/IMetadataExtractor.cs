using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Abstractions.Interfaces;

public interface IMetadataExtractor
{
    Task<FileRecord> EnrichAsync(FileRecord record, CancellationToken cancellationToken = default);
}
