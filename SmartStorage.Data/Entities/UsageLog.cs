namespace SmartStorage.Data.Entities;

public sealed class UsageLog
{
    public long Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public DateTime AccessedUtc { get; set; }
}
