using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartStorage.Application.Services;
using SmartStorage.Core.Indexing;

namespace SmartStorage.UI;

public enum AppPhase
{
    Ready,
    Indexing,
    AnalyzingFromIndex,
    Complete
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ScanService _scanService;
    private readonly RecommendationService _recommendationService;
    private readonly ProjectAnalysisService _projectAnalysisService;
    private readonly FileIndexService _indexService;
    private string _rootPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    private string _status = "Ready";
    private double _progressValue;
    private string _progressText = "0%";
    private AppPhase _currentPhase = AppPhase.Ready;
    private const int MaxLogEntries = 200;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AppPhase CurrentPhase
    {
        get => _currentPhase;
        set { _currentPhase = value; OnPropertyChanged(); }
    }

    public string RootPath
    {
        get => _rootPath;
        set { _rootPath = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public double ProgressValue
    {
        get => _progressValue;
        set { _progressValue = value; OnPropertyChanged(); }
    }

    public string ProgressText
    {
        get => _progressText;
        set { _progressText = value; OnPropertyChanged(); }
    }

    public ObservableCollection<ResultItemViewModel> Results { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();
    public ICommand IndexCommand { get; }
    public ICommand ScanCommand { get; }

    public MainViewModel(ScanService scanService, RecommendationService recommendationService, ProjectAnalysisService projectAnalysisService, FileIndexService indexService)
    {
        _scanService = scanService;
        _recommendationService = recommendationService;
        _projectAnalysisService = projectAnalysisService;
        _indexService = indexService;
        IndexCommand = new AsyncRelayCommand(BuildIndexAsync);
        ScanCommand = new AsyncRelayCommand(AnalyzeFromIndexAsync);
    }

    public async Task BuildIndexAsync()
    {
        if (CurrentPhase != AppPhase.Ready)
        {
            AddLog("⏸ Already processing. Please wait.");
            return;
        }

        Results.Clear();
        Logs.Clear();
        ProgressValue = 0;
        ProgressText = "0%";
        CurrentPhase = AppPhase.Indexing;
        Status = $"🔍 [INDEXING STARTED] Scanning PC files from: {RootPath}";
        AddLog($"═══════════════════════════════════════════════════════════");
        AddLog($"PHASE 1: FILE INDEXING STARTED");
        AddLog($"═══════════════════════════════════════════════════════════");
        AddLog($"Starting comprehensive PC file scan from: {RootPath}");
        AddLog($"This will index all files to the database...");
        AddLog($"");

        try
        {
            int count = 0;
            Status = "🔍 Indexing files...";

            // Phase 1: Scan and index all files
            var items = await _scanService.ScanAsync(RootPath);
            count = items.Count;

            ProgressValue = 100;
            ProgressText = "100%";
            Status = $"✓ [INDEXING COMPLETE] Indexed {count} files";
            AddLog($"");
            AddLog($"✓ Successfully indexed {count} files to database");
            AddLog($"═══════════════════════════════════════════════════════════");
            AddLog($"");
            AddLog($"═══════════════════════════════════════════════════════════");
            AddLog($"PHASE 2: ANALYSIS STARTING");
            AddLog($"═══════════════════════════════════════════════════════════");
            AddLog($"Now analyzing indexed files from database...");

            // Automatically start Phase 2
            await AnalyzeFromIndexAsync();
        }
        catch (Exception ex)
        {
            CurrentPhase = AppPhase.Ready;
            Status = $"✗ Error during indexing: {ex.Message}";
            AddLog($"✗ Indexing failed: {ex.Message}");
        }
    }

    private async Task AnalyzeFromIndexAsync()
    {
        if (CurrentPhase == AppPhase.Ready)
        {
            // If called directly from UI, need to index first
            await BuildIndexAsync();
            return;
        }

        if (CurrentPhase != AppPhase.Indexing)
            return;

        Results.Clear();
        ProgressValue = 0;
        ProgressText = "0%";
        CurrentPhase = AppPhase.AnalyzingFromIndex;
        Status = "📊 Analyzing indexed files...";

        try
        {
            // Get indexed files from database instead of live scan
            var indexedFiles = await _indexService.GetIndexedFilesAsync(RootPath);
            var total = Math.Min(indexedFiles.Count, 5000);

            if (total == 0)
            {
                Status = "✓ Analysis complete: 0 files found.";
                ProgressValue = 100;
                ProgressText = "100%";
                CurrentPhase = AppPhase.Complete;
                AddLog("⚠ No indexed files to analyze.");
                AddLog($"═══════════════════════════════════════════════════════════");
                return;
            }

            var count = 0;
            Status = $"📊 Analyzing {total} indexed file(s)...";
            AddLog($"Database contains {indexedFiles.Count} files. Analyzing first {total}.");
            AddLog($"");

            foreach (var file in indexedFiles.Take(total))
            {
                var itemLabel = file.IsDirectory ? "📁" : "📄";
                AddLog($"{itemLabel} Analyzing: {file.Path}");

                try
                {
                    var recommendation = await _recommendationService.AnalyzeAsync(file);
                    var project = file.IsDirectory ? await _projectAnalysisService.DetectProjectAsync(file.Path) : null;
                    Results.Add(new ResultItemViewModel
                    {
                        Path = file.Path,
                        Kind = recommendation.Kind.ToString(),
                        Score = recommendation.Score.ToString("0.00"),
                        Reason = recommendation.Reason,
                        Tags = string.Join(", ", recommendation.Tags ?? Array.Empty<string>()) + (project is not null ? $" | project:{project.ProjectType}" : string.Empty)
                    });
                    AddLog($"  ✓ → {recommendation.Kind} (Score: {recommendation.Score:0.00})");
                }
                catch (Exception ex)
                {
                    AddLog($"  ✗ Error: {ex.Message}");
                }

                count++;
                ProgressValue = count * 100.0 / total;
                ProgressText = $"{ProgressValue:0}%";
                Status = $"📊 Analyzing {count}/{total} file(s)...";
            }

            ProgressValue = 100;
            ProgressText = "100%";
            CurrentPhase = AppPhase.Complete;
            AddLog($"");
            AddLog($"✓ Analysis complete: {count} items summarized.");
            AddLog($"═══════════════════════════════════════════════════════════");
            Status = $"✓ Analysis complete: {count} item(s) summarized.";
        }
        catch (Exception ex)
        {
            CurrentPhase = AppPhase.Ready;
            Status = $"✗ Error during analysis: {ex.Message}";
            AddLog($"✗ Analysis failed: {ex.Message}");
        }
    }

    private void AddLog(string message)
    {
        Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        while (Logs.Count > MaxLogEntries)
        {
            Logs.RemoveAt(0);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class ResultItemViewModel
{
    public string Path { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Score { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
}

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private bool _isRunning;
    public AsyncRelayCommand(Func<Task> execute) => _execute = execute;
    public bool CanExecute(object? parameter) => !_isRunning;
    public event EventHandler? CanExecuteChanged;
    public async void Execute(object? parameter)
    {
        _isRunning = true; CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try { await _execute(); }
        finally { _isRunning = false; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
}
