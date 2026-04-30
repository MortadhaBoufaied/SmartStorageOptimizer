using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Application.Services;

public sealed class ProjectAnalysisService
{
    private readonly IReadOnlyCollection<IProjectDetector> _detectors;

    public ProjectAnalysisService(IEnumerable<IProjectDetector> detectors)
    {
        _detectors = detectors.ToArray();
    }

    public async Task<ProjectInsight?> DetectProjectAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        foreach (var detector in _detectors)
        {
            var result = await detector.DetectAsync(directoryPath, cancellationToken);
            if (result is not null) return result;
        }
        return null;
    }
}
