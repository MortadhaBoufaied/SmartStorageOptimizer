using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Application.DTOs;

public sealed record AnalysisResultDto(FileDto Item, Recommendation Recommendation, IReadOnlyCollection<string> Tags, string? ProjectType, string? AiLabel);
