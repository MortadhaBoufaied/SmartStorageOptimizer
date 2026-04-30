using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Plugins.ProjectDetectors;

public sealed class FlutterDetector : IProjectDetector
{
    public string Name => "Flutter";

    public Task<ProjectInsight?> DetectAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var pubspec = Path.Combine(directoryPath, "pubspec.yaml");
        if (!File.Exists(pubspec)) return Task.FromResult<ProjectInsight?>(null);
        var cleanable = new List<string>();
        foreach (var candidate in new[] { "build", ".dart_tool", ".flutter-plugins-dependencies" })
        {
            var path = Path.Combine(directoryPath, candidate);
            if (Directory.Exists(path) || File.Exists(path)) cleanable.Add(path);
        }
        return Task.FromResult<ProjectInsight?>(new ProjectInsight(directoryPath, "Flutter", 0.99, new[] { pubspec, Path.Combine(directoryPath, "lib") }, cleanable, new[] { "project", "flutter" }));
    }
}
