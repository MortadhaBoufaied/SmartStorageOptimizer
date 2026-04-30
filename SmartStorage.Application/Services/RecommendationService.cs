using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;
using SmartStorage.Core.Decision;
using SmartStorage.Core.Safety;

namespace SmartStorage.Application.Services;

public sealed class RecommendationService
{
    private readonly IUsageTracker _usageTracker;
    private readonly IRuleEngine _ruleEngine;
    private readonly IScoringEngine _scoringEngine;
    private readonly DecisionEngine _decisionEngine;
    private readonly RiskAnalyzer _riskAnalyzer;
    private readonly IAiAgentClient? _aiAgentClient;

    public RecommendationService(
        IUsageTracker usageTracker,
        IRuleEngine ruleEngine,
        IScoringEngine scoringEngine,
        DecisionEngine decisionEngine,
        RiskAnalyzer riskAnalyzer,
        IAiAgentClient? aiAgentClient = null)
    {
        _usageTracker = usageTracker;
        _ruleEngine = ruleEngine;
        _scoringEngine = scoringEngine;
        _decisionEngine = decisionEngine;
        _riskAnalyzer = riskAnalyzer;
        _aiAgentClient = aiAgentClient;
    }

    public async Task<Recommendation> AnalyzeAsync(FileRecord record, CancellationToken cancellationToken = default)
    {
        var usage = await _usageTracker.GetUsageProfileAsync(record.Path, cancellationToken);
        var tags = await _ruleEngine.EvaluateTagsAsync(record, usage, cancellationToken);
        var score = await _scoringEngine.ScoreAsync(record, usage, tags, cancellationToken);

        if (_aiAgentClient is not null && tags.Contains("document") && score >= 0.45 && score <= 0.75)
        {
            var snippet = TryReadSnippet(record.Path);
            var ai = await _aiAgentClient.AnalyzeAsync(new AiAnalysisRequest(record.Path, record.Extension, snippet, new[] { "important", "archive", "temporary" }), cancellationToken);
            if (ai is not null)
            {
                if (ai.Label.Equals("important", StringComparison.OrdinalIgnoreCase)) score = Math.Max(0, score - 0.25);
                if (ai.Label.Equals("temporary", StringComparison.OrdinalIgnoreCase)) score = Math.Min(1, score + 0.15);
                tags = tags.Concat(ai.Tags ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }
        }

        var recommendation = _decisionEngine.Build(record, score, tags);
        if (_riskAnalyzer.IsHighRisk(record, tags) && recommendation.Kind is RecommendationKind.Delete or RecommendationKind.Clean)
        {
            recommendation = recommendation with { Kind = RecommendationKind.Review, Reason = recommendation.Reason + "; escalated for safety review" };
        }
        return recommendation;
    }

    private static string? TryReadSnippet(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            if (new FileInfo(path).Length > 100_000) return null;
            if (Path.GetExtension(path).ToLowerInvariant() is not ".txt" and not ".md" and not ".log" and not ".json") return null;
            using var reader = new StreamReader(path);
            var buffer = new char[4000];
            var count = reader.Read(buffer, 0, buffer.Length);
            return new string(buffer, 0, count);
        }
        catch { return null; }
    }
}
