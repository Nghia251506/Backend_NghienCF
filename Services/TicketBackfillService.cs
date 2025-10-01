// Services/TicketBackfillService.cs
using Backend_Nghiencf.Data;
using Backend_Nghiencf.Services;
using Microsoft.EntityFrameworkCore;

public sealed class TicketBackfillService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketBackfillService> _log;

    public TicketBackfillService(IServiceScopeFactory scopeFactory, ILogger<TicketBackfillService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Chờ app warm-up
        try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
        catch (OperationCanceledException) { return; }

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var bookingDevSvc = scope.ServiceProvider.GetRequiredService<IBookingDevService>();

                    // Lấy danh sách booking "paid" nhưng chưa có ticket
                    var ids = await db.Bookings
                        .Where(b => b.PaymentStatus == "paid")
                        .Where(b => !db.Tickets.Any(t => t.BookingId == b.Id))
                        .OrderBy(b => b.CreatedAt)
                        .Select(b => b.Id)
                        .Take(50)
                        .ToListAsync(stoppingToken);

                    foreach (var id in ids)
                    {
                        try
                        {
                            await bookingDevSvc.MarkPaidAndIssueTicketsAsync(id, stoppingToken);
                            _log.LogInformation("Backfilled tickets for booking {Id}", id);
                        }
                        catch (OperationCanceledException)
                        {
                            throw; // tôn trọng cancel
                        }
                        catch (Exception ex)
                        {
                            // Không cho phép lỗi thoát ra ngoài vòng lặp
                            _log.LogWarning(ex, "Backfill ticket failed for booking {Id}", id);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Service đang dừng, thoát while ở lần tick tiếp theo
                    break;
                }
                catch (Exception ex)
                {
                    // Lỗi cấp vòng lặp – log lại nhưng KHÔNG ném ra
                    _log.LogError(ex, "TicketBackfillService loop error");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // ignore: service stopping
        }
        catch (Exception ex)
        {
            // Safety: vẫn không để exception “bay” ra khỏi ExecuteAsync
            _log.LogCritical(ex, "TicketBackfillService crashed, but host will continue (ignored).");
        }
    }
}
