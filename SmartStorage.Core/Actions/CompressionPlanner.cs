namespace SmartStorage.Core.Actions;

public sealed class CompressionPlanner
{
    public string BuildArchivePath(string sourcePath, string archiveRoot)
    {
        var name = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar));
        return Path.Combine(archiveRoot, $"{name}-{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
    }
}
