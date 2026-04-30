using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Application.Services;

public sealed class ExecutionService
{
    private readonly IBackupManager _backupManager;
    private readonly ICompressor _compressor;

    public ExecutionService(IBackupManager backupManager, ICompressor compressor)
    {
        _backupManager = backupManager;
        _compressor = compressor;
    }

    public async Task<string> ExecuteAsync(Recommendation recommendation, string archiveRoot, CancellationToken cancellationToken = default)
    {
        var backup = await _backupManager.BackupAsync(recommendation.TargetPath, cancellationToken);

        switch (recommendation.Kind)
        {
            case RecommendationKind.Compress:
                var archive = Path.Combine(archiveRoot, Path.GetFileName(recommendation.TargetPath) + ".zip");
                await _compressor.CompressAsync(recommendation.TargetPath, archive, cancellationToken);
                return $"Compressed. Backup: {backup}. Archive: {archive}";
            case RecommendationKind.Delete:
                if (Directory.Exists(recommendation.TargetPath)) Directory.Delete(recommendation.TargetPath, recursive: true);
                else if (File.Exists(recommendation.TargetPath)) File.Delete(recommendation.TargetPath);
                return $"Deleted. Backup: {backup}";
            default:
                return $"No-op for {recommendation.Kind}. Backup prepared at {backup}";
        }
    }
}
