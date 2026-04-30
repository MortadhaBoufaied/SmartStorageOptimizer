using SmartStorage.Infrastructure.Storage;

namespace SmartStorage.Core.Indexing;

public sealed class FileChangeTracker : IDisposable
{
    private readonly FileRepository _repository;
    private readonly Dictionary<string, FileSystemWatcher> _watchers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _updateDebounce = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _watcherLock = new();
    private const int DebounceDelayMs = 500; // Prevent rapid successive updates

    public FileChangeTracker(FileRepository repository)
    {
        _repository = repository;
    }

    public void StartWatching(string path)
    {
        if (!Directory.Exists(path))
            return;

        lock (_watcherLock)
        {
            if (_watchers.ContainsKey(path))
                return;

            var watcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size,
                IncludeSubdirectories = true,
            };

            watcher.Created += OnFileCreated;
            watcher.Changed += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            watcher.Deleted += OnFileDeleted;
            watcher.Error += OnWatcherError;

            try
            {
                watcher.EnableRaisingEvents = true;
                _watchers[path] = watcher;
            }
            catch
            {
                watcher.Dispose();
                // Silently fail if watcher can't be enabled (e.g., network path)
            }
        }
    }

    public void StopWatching(string path)
    {
        lock (_watcherLock)
        {
            if (_watchers.TryGetValue(path, out var watcher))
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                _watchers.Remove(path);
            }
            _updateDebounce.Remove(path);
        }
    }

    private bool ShouldDebounce(string path)
    {
        if (_updateDebounce.TryGetValue(path, out var lastUpdate))
        {
            if ((DateTime.UtcNow - lastUpdate).TotalMilliseconds < DebounceDelayMs)
            {
                return true;
            }
        }
        _updateDebounce[path] = DateTime.UtcNow;
        return false;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (ShouldDebounce(e.FullPath))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                if (File.Exists(e.FullPath))
                {
                    var info = new FileInfo(e.FullPath);
                    var record = new Abstractions.Contracts.FileRecord(
                        Path: e.FullPath,
                        Name: info.Name,
                        IsDirectory: false,
                        SizeBytes: info.Length,
                        Extension: info.Extension.ToLowerInvariant(),
                        CreatedUtc: info.CreationTimeUtc,
                        ModifiedUtc: info.LastWriteTimeUtc,
                        LastSeenUtc: DateTime.UtcNow,
                        Hash: null
                    );
                    await _repository.UpsertAsync(record);
                }
                else if (Directory.Exists(e.FullPath))
                {
                    var info = new DirectoryInfo(e.FullPath);
                    var record = new Abstractions.Contracts.FileRecord(
                        Path: e.FullPath,
                        Name: info.Name,
                        IsDirectory: true,
                        SizeBytes: 0,
                        Extension: string.Empty,
                        CreatedUtc: info.CreationTimeUtc,
                        ModifiedUtc: info.LastWriteTimeUtc,
                        LastSeenUtc: DateTime.UtcNow,
                        Hash: null
                    );
                    await _repository.UpsertAsync(record);
                }
            }
            catch
            {
                // Silently ignore update failures
            }
        });
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        OnFileCreated(sender, e);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        // Handle rename as delete old + create new
        if (ShouldDebounce(e.FullPath))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                OnFileCreated(sender, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(e.FullPath)!, e.Name!));
            }
            catch { }
        });
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        // Mark as deleted in index on next analysis
        // For now, just leave it; next index refresh will detect it's gone
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        // Watcher buffer overflow or other error - silently ignore
        // This can happen with rapid file changes in large directories
    }

    public void Dispose()
    {
        lock (_watcherLock)
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            _watchers.Clear();
            _updateDebounce.Clear();
        }
    }
}
