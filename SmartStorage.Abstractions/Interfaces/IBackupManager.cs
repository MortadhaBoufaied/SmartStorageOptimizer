namespace SmartStorage.Abstractions.Interfaces;

public interface IBackupManager
{
    Task<string> BackupAsync(string path, CancellationToken cancellationToken = default);
}
