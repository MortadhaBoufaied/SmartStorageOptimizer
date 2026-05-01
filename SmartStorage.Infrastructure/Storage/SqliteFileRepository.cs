using Microsoft.Data.Sqlite;
using SmartStorage.Abstractions.Contracts;

namespace SmartStorage.Infrastructure.Storage;

/// <summary>
/// SQLite-backed repository for file index. High-performance, indexed storage.
/// </summary>
public sealed class SqliteFileRepository
{
    private readonly string _dbPath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _connectionString;

    public SqliteFileRepository(string dbPath)
    {
        _dbPath = dbPath;
        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Files (
                Path TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                IsDirectory INTEGER NOT NULL,
                SizeBytes INTEGER NOT NULL,
                Extension TEXT NOT NULL,
                CreatedUtc TEXT NOT NULL,
                ModifiedUtc TEXT NOT NULL,
                LastSeenUtc TEXT,
                Hash TEXT,
                TagsJson TEXT
            );

            CREATE INDEX IF NOT EXISTS IX_Files_Path ON Files(Path);
            CREATE INDEX IF NOT EXISTS IX_Files_Extension ON Files(Extension);
            CREATE INDEX IF NOT EXISTS IX_Files_SizeBytes ON Files(SizeBytes);
            CREATE INDEX IF NOT EXISTS IX_Files_LastSeenUtc ON Files(LastSeenUtc);
        ";
        command.ExecuteNonQuery();
    }

    public async Task UpsertAsync(FileRecord record, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Files (Path, Name, IsDirectory, SizeBytes, Extension, CreatedUtc, ModifiedUtc, LastSeenUtc, Hash, TagsJson)
                VALUES (@Path, @Name, @IsDirectory, @SizeBytes, @Extension, @CreatedUtc, @ModifiedUtc, @LastSeenUtc, @Hash, @TagsJson)
                ON CONFLICT(Path) DO UPDATE SET
                    Name = excluded.Name,
                    IsDirectory = excluded.IsDirectory,
                    SizeBytes = excluded.SizeBytes,
                    Extension = excluded.Extension,
                    CreatedUtc = excluded.CreatedUtc,
                    ModifiedUtc = excluded.ModifiedUtc,
                    LastSeenUtc = excluded.LastSeenUtc,
                    Hash = excluded.Hash,
                    TagsJson = excluded.TagsJson;
            ";

            command.Parameters.AddWithValue("@Path", record.Path);
            command.Parameters.AddWithValue("@Name", record.Name);
            command.Parameters.AddWithValue("@IsDirectory", record.IsDirectory ? 1 : 0);
            command.Parameters.AddWithValue("@SizeBytes", record.SizeBytes);
            command.Parameters.AddWithValue("@Extension", record.Extension);
            command.Parameters.AddWithValue("@CreatedUtc", record.CreatedUtc.ToString("o"));
            command.Parameters.AddWithValue("@ModifiedUtc", record.ModifiedUtc.ToString("o"));
            command.Parameters.AddWithValue("@LastSeenUtc", record.LastSeenUtc?.ToString("o") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Hash", record.Hash ?? (object)DBNull.Value);

            var tagsJson = record.Tags is null ? null : System.Text.Json.JsonSerializer.Serialize(record.Tags);
            command.Parameters.AddWithValue("@TagsJson", tagsJson ?? (object)DBNull.Value);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UpsertBatchAsync(IEnumerable<FileRecord> records, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Files (Path, Name, IsDirectory, SizeBytes, Extension, CreatedUtc, ModifiedUtc, LastSeenUtc, Hash, TagsJson)
                VALUES (@Path, @Name, @IsDirectory, @SizeBytes, @Extension, @CreatedUtc, @ModifiedUtc, @LastSeenUtc, @Hash, @TagsJson)
                ON CONFLICT(Path) DO UPDATE SET
                    Name = excluded.Name,
                    IsDirectory = excluded.IsDirectory,
                    SizeBytes = excluded.SizeBytes,
                    Extension = excluded.Extension,
                    CreatedUtc = excluded.CreatedUtc,
                    ModifiedUtc = excluded.ModifiedUtc,
                    LastSeenUtc = excluded.LastSeenUtc,
                    Hash = excluded.Hash,
                    TagsJson = excluded.TagsJson;
            ";

            var pathParam = command.Parameters.Add("@Path", SqliteType.Text);
            var nameParam = command.Parameters.Add("@Name", SqliteType.Text);
            var isDirParam = command.Parameters.Add("@IsDirectory", SqliteType.Integer);
            var sizeParam = command.Parameters.Add("@SizeBytes", SqliteType.Integer);
            var extParam = command.Parameters.Add("@Extension", SqliteType.Text);
            var createdParam = command.Parameters.Add("@CreatedUtc", SqliteType.Text);
            var modifiedParam = command.Parameters.Add("@ModifiedUtc", SqliteType.Text);
            var lastSeenParam = command.Parameters.Add("@LastSeenUtc", SqliteType.Text);
            var hashParam = command.Parameters.Add("@Hash", SqliteType.Text);
            var tagsParam = command.Parameters.Add("@TagsJson", SqliteType.Text);

            foreach (var record in records)
            {
                pathParam.Value = record.Path;
                nameParam.Value = record.Name;
                isDirParam.Value = record.IsDirectory ? 1 : 0;
                sizeParam.Value = record.SizeBytes;
                extParam.Value = record.Extension;
                createdParam.Value = record.CreatedUtc.ToString("o");
                modifiedParam.Value = record.ModifiedUtc.ToString("o");
                lastSeenParam.Value = record.LastSeenUtc?.ToString("o") ?? DBNull.Value;
                hashParam.Value = record.Hash ?? DBNull.Value;

                var tagsJson = record.Tags is null ? null : System.Text.Json.JsonSerializer.Serialize(record.Tags);
                tagsParam.Value = tagsJson ?? DBNull.Value;

                await command.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
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
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();

            if (string.IsNullOrEmpty(rootPath))
            {
                command.CommandText = "SELECT Path, Name, IsDirectory, SizeBytes, Extension, CreatedUtc, ModifiedUtc, LastSeenUtc, Hash, TagsJson FROM Files ORDER BY Path";
            }
            else
            {
                var normalizedRoot = Path.GetFullPath(rootPath);
                command.CommandText = "SELECT Path, Name, IsDirectory, SizeBytes, Extension, CreatedUtc, ModifiedUtc, LastSeenUtc, Hash, TagsJson FROM Files WHERE Path LIKE @RootPath ORDER BY Path";
                command.Parameters.AddWithValue("@RootPath", normalizedRoot + "%");
            }

            var records = new List<FileRecord>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var path = reader.GetString(0);
                var name = reader.GetString(1);
                var isDirectory = reader.GetInt32(2) == 1;
                var sizeBytes = reader.GetInt64(3);
                var extension = reader.GetString(4);
                var createdUtc = DateTime.Parse(reader.GetString(5));
                var modifiedUtc = DateTime.Parse(reader.GetString(6));
                var lastSeenUtc = reader.IsDBNull(7) ? (DateTime?)null : DateTime.Parse(reader.GetString(7));
                var hash = reader.IsDBNull(8) ? null : reader.GetString(8);
                var tagsJson = reader.IsDBNull(9) ? null : reader.GetString(9);

                IReadOnlyCollection<string>? tags = null;
                if (!string.IsNullOrEmpty(tagsJson))
                {
                    try { tags = System.Text.Json.JsonSerializer.Deserialize<string[]>(tagsJson); } catch { tags = null; }
                }

                records.Add(new FileRecord(path, name, isDirectory, sizeBytes, extension, createdUtc, modifiedUtc, lastSeenUtc, hash, tags));
            }

            return records.AsReadOnly();
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

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Files WHERE LastSeenUtc IS NOT NULL AND datetime(LastSeenUtc) < datetime(@Cutoff)";
            command.Parameters.AddWithValue("@Cutoff", cutoff.ToString("o"));

            var deletedCount = await command.ExecuteNonQueryAsync(cancellationToken);
            return deletedCount;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<long> GetTotalSizeAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COALESCE(SUM(SizeBytes), 0) FROM Files WHERE IsDirectory = 0";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is long value ? value : 0L;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<int> GetFileCountAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM Files WHERE IsDirectory = 0";

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is long value ? (int)value : 0;
        }
        finally
        {
            _lock.Release();
        }
    }
}