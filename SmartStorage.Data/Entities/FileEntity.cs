namespace SmartStorage.Data.Entities;

public sealed class FileEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDirectory { get; set; }
    public long SizeBytes { get; set; }
    public string Extension { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DateTime ModifiedUtc { get; set; }
    public DateTime? LastSeenUtc { get; set; }
    public string? Hash { get; set; }
    public string TagsJson { get; set; } = "[]";
}
