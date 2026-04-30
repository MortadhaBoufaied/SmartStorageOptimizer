using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Core.Usage;

public sealed class UsageTracker : IUsageTracker
{
    private readonly Dictionary<string, List<DateTime>> _events = new(StringComparer.OrdinalIgnoreCase);

    public Task TrackAccessAsync(string path, DateTime timestampUtc, CancellationToken cancellationToken = default)
    {
        if (!_events.TryGetValue(path, out var values))
        {
            values = new List<DateTime>();
            _events[path] = values;
        }
        values.Add(timestampUtc);
        return Task.CompletedTask;
    }

    public Task<UsageProfile> GetUsageProfileAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!_events.TryGetValue(path, out var values) || values.Count == 0)
        {
            return Task.FromResult(new UsageProfile(path, null, 365, 0, 0.1, 0.0));
        }

        values = values.OrderBy(x => x).ToList();
        var intervals = new List<double>();
        for (var i = 1; i < values.Count; i++) intervals.Add((values[i] - values[i - 1]).TotalDays);
        var avg = intervals.Count == 0 ? 30 : intervals.Average();
        var last = values[^1];
        var recency = Math.Max(0.0, 1.0 - (DateTime.UtcNow - last).TotalDays / Math.Max(7, avg * 4));
        var frequency = Math.Min(1.0, values.Count / 20.0);
        return Task.FromResult(new UsageProfile(path, last, avg, values.Count, recency, frequency));
    }
}
