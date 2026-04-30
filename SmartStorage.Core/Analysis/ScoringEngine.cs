using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Core.Analysis;

public sealed class ScoringEngine : IScoringEngine
{
    public Task<double> ScoreAsync(FileRecord record, UsageProfile usage, IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
    {
        var score = 0.0;
        var inactiveDays = usage.LastAccessUtc is null ? (DateTime.UtcNow - record.ModifiedUtc).TotalDays : (DateTime.UtcNow - usage.LastAccessUtc.Value).TotalDays;
        var avgInterval = Math.Max(usage.AverageAccessIntervalDays, 1.0);
        score += inactiveDays / avgInterval;

        if (tags.Contains("unused")) score += 1.5;
        if (tags.Contains("stale")) score += 1.0;
        if (tags.Contains("temporary") || tags.Contains("empty")) score += 2.0;
        if (tags.Contains("dependency-cache") || tags.Contains("generated-output") || tags.Contains("build-output")) score += 2.5;
        if (tags.Contains("document")) score -= 1.5;
        if (usage.RecencyScore > 0.7) score -= 2.0;
        if (usage.FrequencyScore > 0.6) score -= 1.0;
        if (record.SizeBytes > 500 * 1024 * 1024) score += 0.8;

        score = Math.Clamp(score / 6.0, 0.0, 1.0);
        return Task.FromResult(score);
    }
}
