using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartStorage.Core.Indexing;

namespace SmartStorage.Application.Services;

public sealed class IndexingHostedService : BackgroundService
{
    private readonly FileChangeTracker _changeTracker;
    private readonly ILogger<IndexingHostedService> _logger;
    private readonly string _watchPath;

    public IndexingHostedService(FileChangeTracker changeTracker, ILogger<IndexingHostedService> logger)
    {
        _changeTracker = changeTracker;
        _logger = logger;
        _watchPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting file change tracking for {Path}", _watchPath);
            _changeTracker.StartWatching(_watchPath);
            
            // Keep the service running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("File change tracking cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in file change tracking service");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping file change tracking");
        _changeTracker.StopWatching(_watchPath);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _changeTracker.StopWatching(_watchPath);
        base.Dispose();
    }
}
