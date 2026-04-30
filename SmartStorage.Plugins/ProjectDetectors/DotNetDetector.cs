using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Plugins.ProjectDetectors;

public sealed class DotNetDetector : IProjectDetector
{
    public string Name => ".NET";

    public Task<ProjectInsight?> DetectAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var sln = Directory.EnumerateFiles(directoryPath, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
        var csproj = Directory.EnumerateFiles(directoryPath, "*.csproj", SearchOption.AllDirectories).FirstOrDefault();
        if (sln is null && csproj is null) return Task.FromResult<ProjectInsight?>(null);
        var cleanable = Directory.EnumerateDirectories(directoryPath, "bin", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateDirectories(directoryPath, "obj", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return Task.FromResult<ProjectInsight?>(new ProjectInsight(directoryPath, ".NET", 0.97, new[] { sln ?? string.Empty, csproj ?? string.Empty }, cleanable, new[] { "project", "dotnet" }));
    }
}
