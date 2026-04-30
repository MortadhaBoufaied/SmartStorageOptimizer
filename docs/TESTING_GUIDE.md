# Testing the Indexed Architecture Implementation

## Pre-Test Checklist

- ✅ Build succeeds: `dotnet build SmartStorageOptimizer.sln` (0 errors)
- ✅ All projects compile
- ✅ Files created: FileIndexService.cs, FileChangeTracker.cs, IndexingHostedService.cs
- ✅ ScanService modified to persist files
- ✅ DI container updated with new services
- ✅ Database path: `%LocalAppData%\SmartStorageOptimizer\smartstorage.db`

## Test 1: Verify App Launches & Services Initialize

**Steps**:
1. Run: `dotnet run --project SmartStorage.UI/SmartStorage.UI.csproj`
2. Wait for main window to appear
3. Check no errors in console output

**Expected**:
- App opens with UI visible
- IndexingHostedService starts in background (no UI indication yet)
- Activity log shows initial state

**Validation**: App launches without errors, UI appears

---

## Test 2: Verify Database Gets Created

**Steps**:
1. Run app and let it launch fully
2. Navigate to `%LocalAppData%\SmartStorageOptimizer\`
3. Verify `smartstorage.db` file exists

**Expected**:
- SQLite database file created on app startup (via `EnsureCreatedAsync` in OnStartup)
- File size > 0 bytes (contains schema)

**Validation**: Database file exists in correct location

---

## Test 3: Perform Initial Scan & Verify Persistence

**Steps**:
1. In the app, change RootPath to a small folder (e.g., `C:\Temp` or `C:\Users\[YourName]\Documents`)
2. Click "Analyze" button
3. Wait for scan to complete (watch progress bar reach 100%)
4. After scan finishes, close the app
5. Open the database with SQLite viewer:
   ```powershell
   # Option A: Use SQLite CLI
   sqlite3 %LocalAppData%\SmartStorageOptimizer\smartstorage.db "SELECT COUNT(*) FROM Files;"
   
   # Option B: Use VS Code extension "SQLite"
   # Open the .db file directly in VS Code
   ```

**Expected**:
- Scan completes successfully (progress bar reaches 100%)
- Activity log shows files being analyzed
- Database query returns count > 0 (e.g., "425" files indexed)
- Sample query: `SELECT Path, SizeBytes, ModifiedUtc FROM Files LIMIT 5;` shows file records

**Validation**: Files are persisted to database during scan

**SQL Verification Queries**:
```sql
-- Count indexed files
SELECT COUNT(*) as file_count FROM Files;

-- Show first 5 files
SELECT Path, SizeBytes, Hash, ModifiedUtc FROM Files LIMIT 5;

-- Show database size
SELECT SUM(SizeBytes) as total_size FROM Files;

-- Show files by extension
SELECT Extension, COUNT(*) as count FROM Files GROUP BY Extension ORDER BY count DESC LIMIT 10;

-- Show directories
SELECT COUNT(*) FROM Files WHERE IsDirectory = 1;
```

---

## Test 4: Verify Change Tracking (Background Service)

**Steps**:
1. Run app and perform initial scan (from Test 3)
2. While app is running, create a new file in the scanned directory:
   ```powershell
   New-Item "C:\Temp\TestFile_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt" -ItemType File -Value "test"
   ```
3. Wait 2-3 seconds (FileSystemWatcher event processing)
4. Without restarting the app, check database:
   ```sql
   SELECT Path FROM Files WHERE Path LIKE '%TestFile%';
   ```

**Expected**:
- New file appears in database within a few seconds
- No app restart required
- IndexingHostedService is monitoring and updating DB in background

**Validation**: Change tracker successfully updates database without rescanning

---

## Test 5: Verify Path Exclusion

**Steps**:
1. Run app with RootPath = `C:\` (entire drive scan)
2. Let scan run for ~30 seconds (don't need to wait for completion)
3. Stop scan or let it finish
4. Query database:
   ```sql
   SELECT COUNT(*) FROM Files WHERE Path LIKE 'C:\Windows%';
   SELECT COUNT(*) FROM Files WHERE Path LIKE 'C:\Program Files%';
   ```

**Expected**:
- Count should be 0 (or very few from non-excluded subdirs)
- Windows system files are NOT indexed
- Program Files are NOT indexed

**Validation**: Exclusion rules prevent system path indexing

---

## Test 6: Verify File Enrichment Data

**Steps**:
1. After completing a scan, query:
   ```sql
   SELECT Path, Hash, TagsJson FROM Files 
   WHERE Hash IS NOT NULL AND Hash != ''
   LIMIT 10;
   ```

**Expected**:
- Files have Hash values (SHA256) populated
- TagsJson contains classification tags (JSON array)
- Some files may have NULL hash if they were locked during scan

**Validation**: Metadata enrichment (hashing, classification) is persisted

---

## Test 7: Verify Error Resilience

**Steps**:
1. Run app with RootPath = `C:\Users\[YourName]` (includes system files user can't access)
2. Let scan run to completion
3. Check app didn't crash
4. Check activity log for error messages (should continue despite locked files)

**Expected**:
- Scan completes without crashing
- Locked files are skipped with error handling
- Activity log shows pattern like:
  ```
  [15:23:45] Analyzing: C:\Users\...\ntuser.dat
  [15:23:45] ✗ Error: Access denied to file (skipped)
  [15:23:46] Analyzing: C:\Users\...\Documents\File.txt
  [15:23:47] ✓ Completed: 1234 files analyzed
  ```

**Validation**: App handles locked files gracefully without crashing

---

## Debugging Commands

### View Database Contents (PowerShell)
```powershell
$dbPath = "$env:LOCALAPPDATA\SmartStorageOptimizer\smartstorage.db"
sqlite3 $dbPath ".tables"
sqlite3 $dbPath ".schema Files"
sqlite3 $dbPath "SELECT COUNT(*) FROM Files;"
```

### Run App with Console Output
```powershell
cd C:\path\to\SmartStorageOptimizer
dotnet run --project SmartStorage.UI/SmartStorage.UI.csproj 2>&1 | Tee-Object -FilePath scan.log
```

### Check Watcher Events (Monitor FileChangeTracker)
The IndexingHostedService logs to standard output:
```
[info] Starting file change tracking for C:\Users\[YourName]
[info] Stopping file change tracking
```

### Verify DI Container
To check if all services are registered properly, add to App.xaml.cs OnStartup (temporary):
```csharp
var indexService = scope.ServiceProvider.GetRequiredService<FileIndexService>();
var tracker = scope.ServiceProvider.GetRequiredService<FileChangeTracker>();
var repo = scope.ServiceProvider.GetRequiredService<FileRepository>();
Debug.WriteLine("✓ All indexing services initialized");
```

---

## Success Criteria

- ✅ App launches without errors
- ✅ Database file created automatically
- ✅ Files are indexed to database during scan
- ✅ Change tracker updates database when files are created/modified
- ✅ System paths are excluded from indexing
- ✅ File metadata (hash, tags) is persisted
- ✅ App handles locked files gracefully
- ✅ Background service runs without user interaction

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Database not created | Check `%LocalAppData%\SmartStorageOptimizer` folder permissions |
| Files table is empty after scan | Ensure ScanService has FileRepository injected, verify no exceptions in logs |
| Change tracker not updating | Check FileChangeTracker.StartWatching is called in IndexingHostedService |
| Path exclusion not working | Verify FileIndexService.IsPathAllowed logic matches your test paths |
| App crashes on locked files | Verify MetadataExtractor has try-catch for IOException |

---

## Next Steps After Validation

1. Add UI button for "Analyze from Index" mode
2. Implement index rebuild/clear commands
3. Add configuration screen for custom exclusion rules
4. Create backup/restore functionality
5. Add index statistics display in UI
