namespace SmartStorage.Application.DTOs;

public sealed record FileDto(string Path, string Name, long SizeBytes, string Extension, DateTime ModifiedUtc, bool IsDirectory);
