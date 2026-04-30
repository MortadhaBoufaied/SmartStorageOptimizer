using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Plugins.ProjectDetectors;

public sealed class NodeDetector : IProjectDetector
{
    public string Name => "Node.js";

    public Task<ProjectInsight?> DetectAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var packageJson = Path.Combine(directoryPath, "package.json");
        if (!File.Exists(packageJson)) return Task.FromResult<ProjectInsight?>(null);
        var cleanable = new List<string>();
        foreach (var candidate in new[] { "node_modules", "dist", ".next", "coverage", ".turbo" })
        {
            var path = Path.Combine(directoryPath, candidate);
            if (Directory.Exists(path)) cleanable.Add(path);
        }
        return Task.FromResult<ProjectInsight?>(new ProjectInsight(directoryPath, "Node.js", 0.98, new[] { packageJson, Path.Combine(directoryPath, "src") }, cleanable, new[] { "project", "node" }));
    }
}
