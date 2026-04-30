using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Abstractions.Interfaces;

public interface IRuleEngine
{
    Task<IReadOnlyCollection<string>> EvaluateTagsAsync(FileRecord record, UsageProfile usage, CancellationToken cancellationToken = default);
}
