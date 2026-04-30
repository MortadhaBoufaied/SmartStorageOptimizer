using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Core.Analysis;

public sealed class RuleEngine : IRuleEngine
{
    public Task<IReadOnlyCollection<string>> EvaluateTagsAsync(FileRecord record, UsageProfile usage, CancellationToken cancellationToken = default)
    {
        var tags = new HashSet<string>(record.Tags ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var daysInactive = usage.LastAccessUtc is null ? (DateTime.UtcNow - record.ModifiedUtc).TotalDays : (DateTime.UtcNow - usage.LastAccessUtc.Value).TotalDays;

        if (record.IsDirectory) tags.Add("directory");
        if (daysInactive > 365) tags.Add("stale");
        if (daysInactive > 90) tags.Add("cold");
        if (usage.AccessCount == 0 && daysInactive > 180) tags.Add("unused");
        if (!record.IsDirectory && record.SizeBytes == 0) tags.Add("empty");
        if (record.Name.Equals("node_modules", StringComparison.OrdinalIgnoreCase)) tags.Add("dependency-cache");
        if (record.Name.Equals("bin", StringComparison.OrdinalIgnoreCase) || record.Name.Equals("obj", StringComparison.OrdinalIgnoreCase)) tags.Add("build-output");
        if (record.Name.Equals(".venv", StringComparison.OrdinalIgnoreCase) || record.Name.Equals("venv", StringComparison.OrdinalIgnoreCase)) tags.Add("environment-cache");
        if (record.Name.Equals("build", StringComparison.OrdinalIgnoreCase) || record.Name.Equals("dist", StringComparison.OrdinalIgnoreCase)) tags.Add("generated-output");

        return Task.FromResult<IReadOnlyCollection<string>>(tags.ToArray());
    }
}
