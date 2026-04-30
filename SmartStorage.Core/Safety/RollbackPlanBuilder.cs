using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Core.Safety;

public sealed class RollbackPlanBuilder
{
    public string BuildPlan(Recommendation recommendation)
        => $"Backup target before {recommendation.Kind}; keep action log and restore path mapping for {recommendation.TargetPath}";
}
