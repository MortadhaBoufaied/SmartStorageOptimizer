namespace SmartStorage.Abstractions.Contracts;

public sealed record ProjectInsight(
    string RootPath,
    string ProjectType,
    double Confidence,
    IReadOnlyCollection<string> ImportantPaths,
    IReadOnlyCollection<string> CleanablePaths,
    IReadOnlyCollection<string> Tags);
