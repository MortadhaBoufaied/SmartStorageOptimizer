using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Plugins.ProjectDetectors;

public sealed class PythonDetector : IProjectDetector
{
    public string Name => "Python";

    public Task<ProjectInsight?> DetectAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        bool hasPy = File.Exists(Path.Combine(directoryPath, "pyproject.toml")) || File.Exists(Path.Combine(directoryPath, "requirements.txt")) || Directory.EnumerateFiles(directoryPath, "*.py", SearchOption.TopDirectoryOnly).Any();
        if (!hasPy) return Task.FromResult<ProjectInsight?>(null);
        var cleanable = new List<string>();
        foreach (var candidate in new[] { "__pycache__", ".pytest_cache", ".mypy_cache", ".venv", "venv", "dist", "build" })
        {
            var path = Path.Combine(directoryPath, candidate);
            if (Directory.Exists(path)) cleanable.Add(path);
        }
        return Task.FromResult<ProjectInsight?>(new ProjectInsight(directoryPath, "Python", 0.95, new[] { Path.Combine(directoryPath, "pyproject.toml"), Path.Combine(directoryPath, "src") }, cleanable, new[] { "project", "python" }));
    }
}
