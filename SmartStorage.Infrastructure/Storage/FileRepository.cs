using SmartStorage.Abstractions.Contracts;
using System.Text.Json;

namespace SmartStorage.Infrastructure.Storage;

/// <summary>
/// CSV-backed repository for file index. Simple, process-local implementation.
/// </summary>
public sealed class FileRepository
{
    private readonly string _csvPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private const string Header = "Path,Name,IsDirectory,SizeBytes,Extension,CreatedUtc,ModifiedUtc,LastSeenUtc,Hash,TagsJson";

    public FileRepository(string csvPath)
    {
        _csvPath = csvPath;
        var dir = Path.GetDirectoryName(_csvPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        if (!File.Exists(_csvPath)) File.WriteAllText(_csvPath, Header + Environment.NewLine);
    }

    public async Task UpsertAsync(FileRecord record, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var map = ReadAll();
            map[record.Path] = record;
            WriteAll(map.Values);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<FileRecord>> GetIndexedFilesAsync(string? rootPath = null, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var all = ReadAll().Values;
            if (string.IsNullOrEmpty(rootPath)) return all.OrderBy(f => f.Path).ToList().AsReadOnly();
            var normalizedRoot = Path.GetFullPath(rootPath);
            return all.Where(f => f.Path.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                      .OrderBy(f => f.Path)
                      .ToList()
                      .AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<int> DeleteStaleEntriesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var cutoff = DateTime.UtcNow.Subtract(maxAge);
            var map = ReadAll();
            var toRemove = map.Values.Where(f => f.LastSeenUtc.HasValue && f.LastSeenUtc.Value < cutoff).Select(f => f.Path).ToList();
            foreach (var p in toRemove) map.Remove(p);
            if (toRemove.Count > 0) WriteAll(map.Values);
            return toRemove.Count;
        }
        finally
        {
            _lock.Release();
        }
    }

    // --- Helpers ---
    private Dictionary<string, FileRecord> ReadAll()
    {
        var dict = new Dictionary<string, FileRecord>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(_csvPath)) return dict;
        using var sr = new StreamReader(_csvPath);
        var headerLine = sr.ReadLine();
        while (!sr.EndOfStream)
        {
            var line = sr.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = ParseCsvLine(line);
            if (fields.Count < 10) continue;
            var path = fields[0];
            var name = fields[1];
            var isDir = bool.TryParse(fields[2], out var d) && d;
            var size = long.TryParse(fields[3], out var s) ? s : 0L;
            var extension = fields[4];
            var created = DateTime.Parse(fields[5]);
            var modified = DateTime.Parse(fields[6]);
            var lastSeen = string.IsNullOrEmpty(fields[7]) ? (DateTime?)null : DateTime.Parse(fields[7]);
            var hash = string.IsNullOrEmpty(fields[8]) ? null : fields[8];
            var tagsJson = string.IsNullOrEmpty(fields[9]) ? null : fields[9];
            IReadOnlyCollection<string>? tags = null;
            if (!string.IsNullOrEmpty(tagsJson))
            {
                try { tags = JsonSerializer.Deserialize<string[]>(tagsJson); } catch { tags = null; }
            }

            var rec = new FileRecord(path, name, isDir, size, extension, created, modified, lastSeen, hash, tags);
            dict[path] = rec;
        }
        return dict;
    }

    private void WriteAll(IEnumerable<FileRecord> records)
    {
        using var sw = new StreamWriter(_csvPath, false);
        sw.WriteLine(Header);
        foreach (var r in records)
        {
            var tagsJson = r.Tags is null ? string.Empty : JsonSerializer.Serialize(r.Tags);
            var line = string.Join(",",
                EscapeCsv(r.Path),
                EscapeCsv(r.Name),
                r.IsDirectory.ToString(),
                r.SizeBytes.ToString(),
                EscapeCsv(r.Extension),
                EscapeCsv(r.CreatedUtc.ToString("o")),
                EscapeCsv(r.ModifiedUtc.ToString("o")),
                EscapeCsv(r.LastSeenUtc?.ToString("o") ?? string.Empty),
                EscapeCsv(r.Hash ?? string.Empty),
                EscapeCsv(tagsJson)
            );
            sw.WriteLine(line);
        }
    }

    private static string EscapeCsv(string input)
    {
        if (input is null) return string.Empty;
        if (input.Contains('"')) input = input.Replace("\"", "\"\"");
        if (input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r'))
            return '"' + input + '"';
        return input;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        if (string.IsNullOrEmpty(line)) return fields;
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++; // skip escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        fields.Add(sb.ToString());
        return fields;
    }
}
