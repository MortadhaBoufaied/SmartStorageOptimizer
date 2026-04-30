namespace SmartStorage.Abstractions.Interfaces;

public interface ICompressor
{
    string Name { get; }
    Task<string> CompressAsync(string sourcePath, string destinationArchivePath, CancellationToken cancellationToken = default);
}
