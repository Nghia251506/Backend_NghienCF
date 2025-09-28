// Services/TicketBackfillService.cs
using Backend_Nghiencf.Data;
using Backend_Nghiencf.Services;
using Microsoft.EntityFrameworkCore;
public class TicketBackfillService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TicketBackfillService> _log;
    public TicketBackfillService(IServiceScopeFactory scopeFactory, ILogger<TicketBackfillService> log)
    {
        _scopeFactory = scopeFactory; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var bookingSvc = scope.ServiceProvider.GetRequiredService<IBookingDevService>();

                var ids = await db.Bookings
                    .Where(b => b.PaymentStatus == "paid")
                    .Where(b => !db.Tickets.Any(t => t.BookingId == b.Id))
                    .OrderBy(b => b.CreatedAt)
                    .Select(b => b.Id)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                foreach (var id in ids)
                {
                    try { await bookingSvc.MarkPaidAndIssueTicketsAsync(id, stoppingToken); }
                    catch (Exception ex) { _log.LogWarning(ex, "Backfill ticket failed for {Id}", id); }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "TicketBackfillService loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
