namespace SmartStorage.Abstractions.Contracts;

public sealed record AiAnalysisRequest(string Path, string? Extension, string? Snippet, IReadOnlyCollection<string>? CandidateLabels);
public sealed record AiAnalysisResponse(string Path, string Label, double Confidence, IReadOnlyCollection<string>? Tags, string? Explanation);
