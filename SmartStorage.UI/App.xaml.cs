using System.Windows;
using System.IO;
// SQLite removed: using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartStorage.Abstractions.Interfaces;
using SmartStorage.AI;
using SmartStorage.Application.Services;
using SmartStorage.Core.Analysis;
using SmartStorage.Core.Decision;
using SmartStorage.Core.Indexing;
using SmartStorage.Core.Metadata;
using SmartStorage.Core.Scanner;
using SmartStorage.Core.Safety;
using SmartStorage.Core.Usage;
using SmartStorage.Data;
using SmartStorage.Infrastructure.FileSystem;
using SmartStorage.Infrastructure.Storage;
using SmartStorage.Plugins.Cleaners;
using SmartStorage.Plugins.ProjectDetectors;

namespace SmartStorage.UI;

public partial class App : System.Windows.Application
{
    public static IHost HostContainer { get; private set; } = null!;

    public App()
    {
        HostContainer = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((ctx, services) =>
            {
                var storageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartStorageOptimizer");
                Directory.CreateDirectory(storageDir);
                var csvPath = Path.Combine(storageDir, "index.csv");

                services.AddSingleton<FileRepository>(_ => new FileRepository(csvPath));
                services.AddSingleton<IFileScanner, FileScanner>();
                services.AddSingleton<IMetadataExtractor, MetadataExtractor>();
                services.AddSingleton<IUsageTracker, UsageTracker>();
                services.AddSingleton<IRuleEngine, RuleEngine>();
                services.AddSingleton<IScoringEngine, ScoringEngine>();
                services.AddSingleton<DecisionEngine>();
                services.AddSingleton<RiskAnalyzer>();
                services.AddSingleton<IBackupManager, BackupManager>();
                services.AddSingleton<ICompressor, ZipCompressor>();
                services.AddSingleton<IProjectDetector, NodeDetector>();
                services.AddSingleton<IProjectDetector, PythonDetector>();
                services.AddSingleton<IProjectDetector, FlutterDetector>();
                services.AddSingleton<IProjectDetector, DotNetDetector>();
                services.AddSingleton<NodeCleaner>();
                services.AddSingleton<PythonCleaner>();
                services.AddSingleton<FlutterCleaner>();
                services.AddSingleton<DotNetCleaner>();
                // Read optional excluded paths from configuration and combine with defaults in FileIndexService
                var configured = ctx.Configuration.GetSection("Indexing:ExcludedPaths").Get<string[]>() ?? Array.Empty<string>();
                services.AddSingleton<FileIndexService>(sp => new FileIndexService(
                    sp.GetRequiredService<FileRepository>(),
                    sp.GetRequiredService<IFileScanner>(),
                    sp.GetRequiredService<IMetadataExtractor>(),
                    sp,
                    configured
                ));
                services.AddSingleton<FileChangeTracker>();
                services.AddSingleton<ScanService>();
                services.AddSingleton<ProjectAnalysisService>();
                services.AddSingleton<RecommendationService>();
                services.AddSingleton<ExecutionService>();
                services.AddHostedService<AgentHostedService>();
                // Temporarily disabled: IndexingHostedService causes excessive watcher events
                // services.AddHostedService<IndexingHostedService>();
                services.AddSingleton<MainViewModel>();
                services.AddSingleton<MainWindow>();

                // Optional AI client registration (uncomment and configure on Windows with bundled Python)
                // services.AddSingleton<IAiAgentClient>(_ => new PythonAgentClient(@"python\python.exe", @"python_ai\agent.py"));
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await HostContainer.StartAsync();
        // No database initialization required for CSV-backed index.
        // (previously ensured SQLite DB was created here)
        var window = HostContainer.Services.GetRequiredService<MainWindow>();
        window.Show();
        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await HostContainer.StopAsync();
        HostContainer.Dispose();
        base.OnExit(e);
    }
}
