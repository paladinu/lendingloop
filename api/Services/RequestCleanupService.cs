namespace Api.Services;

public class RequestCleanupService : IHostedService, IDisposable
{
    private readonly ILogger<RequestCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;
    private readonly TimeSpan _cleanupInterval;

    public RequestCleanupService(ILogger<RequestCleanupService> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        // Configure cleanup interval - default to 24 hours (1 day)
        var intervalHours = configuration.GetValue<int>("RequestCleanup:IntervalHours", 24);
        _cleanupInterval = TimeSpan.FromHours(intervalHours);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RequestCleanupService is starting. Cleanup interval: {Interval}", _cleanupInterval);

        // Execute cleanup immediately on startup
        await ExecuteCleanupAsync();

        // Schedule the timer to run every day after the initial execution
        _timer = new Timer(ExecuteCleanup, null, _cleanupInterval, _cleanupInterval);
    }

    private async Task ExecuteCleanupAsync()
    {
        try
        {
            _logger.LogInformation("Starting request cleanup process");

            using var scope = _serviceProvider.CreateScope();
            var itemRequestService = scope.ServiceProvider.GetRequiredService<IItemRequestService>();

            await itemRequestService.CancelExpiredRequestsAsync();

            _logger.LogInformation("Request cleanup process completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during request cleanup process");
        }
    }

    private async void ExecuteCleanup(object? state)
    {
        await ExecuteCleanupAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RequestCleanupService is stopping");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}