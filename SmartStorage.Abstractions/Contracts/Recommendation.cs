namespace SmartStorage.Abstractions.Contracts;

public enum RecommendationKind
{
    Keep,
    Compress,
    Clean,
    Delete,
    Review
}

public sealed record Recommendation(
    string TargetPath,
    RecommendationKind Kind,
    double Score,
    string Reason,
    IReadOnlyCollection<string>? Tags = null,
    bool RequiresConfirmation = true,
    bool Reversible = true);
