using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Abstractions.Interfaces;

public interface IUsageTracker
{
    Task TrackAccessAsync(string path, DateTime timestampUtc, CancellationToken cancellationToken = default);
    Task<UsageProfile> GetUsageProfileAsync(string path, CancellationToken cancellationToken = default);
}
