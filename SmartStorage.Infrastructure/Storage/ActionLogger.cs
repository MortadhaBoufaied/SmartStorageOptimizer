using SmartStorage.Data;
using SmartStorage.Data.Entities;

namespace SmartStorage.Infrastructure.Storage;

public sealed class ActionLogger
{
    private readonly AppDbContext _db;
    public ActionLogger(AppDbContext db) => _db = db;

    public async Task LogAsync(string targetPath, string actionKind, bool succeeded, string? backupPath, string? details, CancellationToken cancellationToken = default)
    {
        _db.ActionLogs.Add(new ActionLog
        {
            TargetPath = targetPath,
            ActionKind = actionKind,
            ExecutedUtc = DateTime.UtcNow,
            Succeeded = succeeded,
            BackupPath = backupPath,
            Details = details,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
