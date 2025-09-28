using Microsoft.EntityFrameworkCore;
using Backend_Nghiencf.Data;

public class PendingBookingExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _sf;
    private readonly ILogger<PendingBookingExpiryService> _logger;
    private const int TTL_MIN = 15;
    private const int BATCH = 200;

    public PendingBookingExpiryService(IServiceScopeFactory sf, ILogger<PendingBookingExpiryService> logger)
    { _sf = sf; _logger = logger; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sf.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var cutoff = DateTime.UtcNow.AddMinutes(-TTL_MIN);
                var list = await db.Bookings
                    .Where(b => b.PaymentStatus == "pending" && b.CreatedAt < cutoff)
                    .OrderBy(b => b.CreatedAt)
                    .Take(BATCH)
                    .ToListAsync(stoppingToken);

                if (list.Count > 0)
                {
                    await using var tx = await db.Database.BeginTransactionAsync(stoppingToken);
                    foreach (var b in list)
                    {
                        if (b.PaymentStatus != "pending") continue;

                        // hoàn kho
                        await db.Database.ExecuteSqlRawAsync(@"
                            UPDATE TicketTypes
                            SET RemainingQuantity = RemainingQuantity + {0}
                            WHERE Id = {1} AND ShowId = {2}
                        ", b.Quantity, b.TicketTypeId, b.ShowId, stoppingToken);

                        b.PaymentStatus = "failed";
                        b.PaymentTime   = DateTime.UtcNow;
                    }
                    await db.SaveChangesAsync(stoppingToken);
                    await tx.CommitAsync(stoppingToken);

                    _logger.LogInformation("Expired {count} bookings > {ttl}m", list.Count, TTL_MIN);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expire pending bookings failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
