using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Core.Decision;

public sealed class DecisionEngine
{
    public Recommendation Build(FileRecord record, double score, IReadOnlyCollection<string> tags)
    {
        var reason = new List<string>();
        RecommendationKind kind;

        if (tags.Contains("dependency-cache") || tags.Contains("generated-output") || tags.Contains("build-output") || tags.Contains("environment-cache"))
        {
            kind = RecommendationKind.Clean;
            reason.Add("Generated/build/dependency cache detected");
        }
        else if (score >= 0.85 && (tags.Contains("temporary") || tags.Contains("empty") || tags.Contains("unused")))
        {
            kind = RecommendationKind.Delete;
            reason.Add("Very low utility with disposable characteristics");
        }
        else if (score >= 0.65)
        {
            kind = RecommendationKind.Compress;
            reason.Add("Cold item worth archiving/compressing");
        }
        else if (score >= 0.45)
        {
            kind = RecommendationKind.Review;
            reason.Add("Needs user review because confidence is moderate");
        }
        else
        {
            kind = RecommendationKind.Keep;
            reason.Add("Frequently used or contextually important");
        }

        return new Recommendation(record.Path, kind, score, string.Join("; ", reason), tags, RequiresConfirmation: kind != RecommendationKind.Keep, Reversible: true);
    }
}
