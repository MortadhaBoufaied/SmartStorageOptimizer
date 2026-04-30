namespace SmartStorage.Data.Entities;

public sealed class RecommendationLog
{
    public long Id { get; set; }
    public string TargetPath { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public double Score { get; set; }
    public DateTime LoggedUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}
