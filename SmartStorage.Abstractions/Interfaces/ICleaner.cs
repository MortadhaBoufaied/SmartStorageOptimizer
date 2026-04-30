using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Abstractions.Interfaces;

public interface ICleaner
{
    string Name { get; }
    bool CanHandle(ProjectInsight insight);
    Task<CleanResult> PreviewAsync(ProjectInsight insight, CancellationToken cancellationToken = default);
    Task<CleanResult> ExecuteAsync(ProjectInsight insight, CancellationToken cancellationToken = default);
}
