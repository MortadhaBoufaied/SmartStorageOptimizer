namespace SmartStorage.Abstractions.Contracts;

public sealed record UsageProfile(
    string Path,
    DateTime? LastAccessUtc,
    double AverageAccessIntervalDays,
    int AccessCount,
    double RecencyScore,
    double FrequencyScore);
