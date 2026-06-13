namespace Gamestore.Services;

public class PeriodicService : BackgroundService
{
    private ILogger<PeriodicService> _logger;
    private AuthService _authService;
    public PeriodicService(ILogger<PeriodicService> logger, AuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PeriodicService started");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            _authService.CheckDateTime();
        }
    }
}
