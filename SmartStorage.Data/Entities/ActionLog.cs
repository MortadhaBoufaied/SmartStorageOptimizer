namespace SmartStorage.Data.Entities;

public sealed class ActionLog
{
    public long Id { get; set; }
    public string TargetPath { get; set; } = string.Empty;
    public string ActionKind { get; set; } = string.Empty;
    public DateTime ExecutedUtc { get; set; }
    public bool Succeeded { get; set; }
    public string? BackupPath { get; set; }
    public string? Details { get; set; }
}
