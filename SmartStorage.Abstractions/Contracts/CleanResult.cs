namespace SmartStorage.Abstractions.Contracts;

public sealed record CleanResult(string TargetPath, long EstimatedBytesReclaimed, IReadOnlyCollection<string> DeletedPaths, string Summary);
