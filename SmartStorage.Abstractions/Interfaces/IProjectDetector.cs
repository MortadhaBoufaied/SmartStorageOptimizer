using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Abstractions.Interfaces;

public interface IProjectDetector
{
    string Name { get; }
    Task<ProjectInsight?> DetectAsync(string directoryPath, CancellationToken cancellationToken = default);
}
