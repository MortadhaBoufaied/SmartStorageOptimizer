using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartStorage.Application.Services;

public sealed class AgentHostedService : BackgroundService
{
    private readonly ILogger<AgentHostedService> _logger;
    public AgentHostedService(ILogger<AgentHostedService> logger) => _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Smart Storage background agent started");
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            _logger.LogDebug("Heartbeat from storage agent");
        }
    }
}
