using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Infrastructure.FileSystem;

public sealed class BackupManager : IBackupManager
{
    private readonly string _backupRoot;

    public BackupManager(string? backupRoot = null)
    {
        _backupRoot = backupRoot ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartStorageOptimizer", "backups");
        Directory.CreateDirectory(_backupRoot);
    }

    public async Task<string> BackupAsync(string path, CancellationToken cancellationToken = default)
    {
        var targetName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
        var backupPath = Path.Combine(_backupRoot, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{targetName}");

        if (Directory.Exists(path))
        {
            CopyDirectory(path, backupPath);
        }
        else if (File.Exists(path))
        {
            await using var source = File.OpenRead(path);
            await using var dest = File.Create(backupPath);
            await source.CopyToAsync(dest, cancellationToken);
        }
        return backupPath;
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        Directory.CreateDirectory(destinationDir);
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, file);
            var destination = Path.Combine(destinationDir, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }
    }
}
