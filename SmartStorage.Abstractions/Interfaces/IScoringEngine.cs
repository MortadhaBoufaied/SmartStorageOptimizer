using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Abstractions.Interfaces;

public interface IScoringEngine
{
    Task<double> ScoreAsync(FileRecord record, UsageProfile usage, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default);
}
