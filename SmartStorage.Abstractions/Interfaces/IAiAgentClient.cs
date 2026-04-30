using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Abstractions.Interfaces;

public interface IAiAgentClient : IAsyncDisposable
{
    Task<AiAnalysisResponse?> AnalyzeAsync(AiAnalysisRequest request, CancellationToken cancellationToken = default);
}
