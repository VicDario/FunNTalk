using FunNTalk.Domain.Repositories;
using FunNTalk.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FunNTalk.Infrastructure.Services;

/// <summary>
/// Reclaims rooms that have sat vacant past their TTL. The sweep never reclaims early — it only
/// bounds lateness by one interval, because the reap decision compares against the vacancy
/// stamp, never against when the timer fired. Late reclamation is harmless; early reclamation
/// would break the reconnect grace window.
/// </summary>
internal sealed class RoomReaperService(
    IChatRoomRepository repository,
    TimeProvider timeProvider,
    IOptions<RoomOptions> options,
    ILogger<RoomReaperService> logger) : BackgroundService
{
    private readonly IChatRoomRepository _repository = repository;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(options.Value.SweepIntervalSeconds);
    private readonly TimeSpan _vacancyTtl = TimeSpan.FromMinutes(options.Value.VacancyTtlMinutes);
    private readonly ILogger<RoomReaperService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval, _timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken)) SweepOnce();
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown, not a failure — StopAsync cancels stoppingToken to unblock the loop.
        }
    }

    /// <summary>
    /// A BackgroundService that throws stops the whole host by default (.NET 6+), so every
    /// repository failure is caught and logged here instead of propagating out of the loop.
    /// </summary>
    internal int SweepOnce()
    {
        try
        {
            return _repository.ReapVacantRooms(_vacancyTtl);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Room sweep failed.");
            return 0;
        }
    }
}
