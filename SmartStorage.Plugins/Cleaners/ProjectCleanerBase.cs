using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Plugins.Cleaners;

public abstract class ProjectCleanerBase : ICleaner
{
    public abstract string Name { get; }
    public abstract bool CanHandle(ProjectInsight insight);

    public Task<CleanResult> PreviewAsync(ProjectInsight insight, CancellationToken cancellationToken = default)
    {
        var size = EstimateBytes(insight.CleanablePaths);
        return Task.FromResult(new CleanResult(insight.RootPath, size, insight.CleanablePaths.ToArray(), $"Preview cleanup for {insight.ProjectType}"));
    }

    public Task<CleanResult> ExecuteAsync(ProjectInsight insight, CancellationToken cancellationToken = default)
    {
        var deleted = new List<string>();
        foreach (var path in insight.CleanablePaths)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
                deleted.Add(path);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
                deleted.Add(path);
            }
        }
        var size = EstimateBytes(deleted);
        return Task.FromResult(new CleanResult(insight.RootPath, size, deleted, $"Removed {deleted.Count} generated paths"));
    }

    private static long EstimateBytes(IEnumerable<string> paths)
    {
        long total = 0;
        foreach (var path in paths)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                        total += new FileInfo(file).Length;
                }
                else if (File.Exists(path))
                {
                    total += new FileInfo(path).Length;
                }
            }
            catch { }
        }
        return total;
    }
}
