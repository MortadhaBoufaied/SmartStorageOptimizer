using SmartStorage.Abstractions.Contracts;
using SmartStorage.Abstractions.Interfaces;

namespace SmartStorage.Core.Scanner;

public sealed class FileScanner : IFileScanner
{
    public async IAsyncEnumerable<FileRecord> ScanAsync(string rootPath, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = pending.Pop();

            DirectoryInfo? directory = null;
            try { directory = new DirectoryInfo(current); }
            catch { }
            if (directory is null || !directory.Exists) continue;

            yield return new FileRecord(
                Path: directory.FullName,
                Name: directory.Name,
                IsDirectory: true,
                SizeBytes: 0,
                Extension: string.Empty,
                CreatedUtc: directory.CreationTimeUtc,
                ModifiedUtc: directory.LastWriteTimeUtc,
                LastSeenUtc: DateTime.UtcNow,
                Hash: null);

            IEnumerable<string> subDirs = Array.Empty<string>();
            IEnumerable<string> files = Array.Empty<string>();
            try { subDirs = Directory.EnumerateDirectories(current); } catch { }
            try { files = Directory.EnumerateFiles(current); } catch { }

            foreach (var sub in subDirs) pending.Push(sub);
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                FileInfo? info = null;
                try { info = new FileInfo(file); } catch { }
                if (info is null || !info.Exists) continue;
                yield return new FileRecord(
                    Path: info.FullName,
                    Name: info.Name,
                    IsDirectory: false,
                    SizeBytes: info.Length,
                    Extension: info.Extension.ToLowerInvariant(),
                    CreatedUtc: info.CreationTimeUtc,
                    ModifiedUtc: info.LastWriteTimeUtc,
                    LastSeenUtc: DateTime.UtcNow,
                    Hash: null);
            }
            await Task.Yield();
        }
    }
}
