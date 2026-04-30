using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Abstractions.Interfaces;

public interface IFileScanner
{
    IAsyncEnumerable<FileRecord> ScanAsync(string rootPath, CancellationToken cancellationToken = default);
}
