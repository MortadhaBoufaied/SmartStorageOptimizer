namespace SmartStorage.Abstractions.Contracts;

public sealed record FileRecord(
    string Path,
    string Name,
    bool IsDirectory,
    long SizeBytes,
    string Extension,
    DateTime CreatedUtc,
    DateTime ModifiedUtc,
    DateTime? LastSeenUtc,
    string? Hash,
    IReadOnlyCollection<string>? Tags = null);
