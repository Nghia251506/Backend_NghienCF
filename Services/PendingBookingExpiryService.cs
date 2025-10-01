using Backend_Nghiencf.Data;
using Microsoft.EntityFrameworkCore;

public sealed class PendingBookingExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingBookingExpiryService> _log;
    private const int GraceMinutes = 15; // thời gian chờ thanh toán

    public PendingBookingExpiryService(IServiceScopeFactory scopeFactory, ILogger<PendingBookingExpiryService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
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

                // So sánh trực tiếp với cutoff thay vì dùng EF.Functions
                var cutoff = DateTime.UtcNow.AddMinutes(-GraceMinutes);

                var expired = await db.Bookings
                    .Where(b => b.PaymentStatus == "pending"
                             && b.CreatedAt != null
                             && b.CreatedAt <= cutoff)
                    .OrderBy(b => b.CreatedAt)
                    .Take(200)
                    .ToListAsync(stoppingToken);

                foreach (var b in expired)
                {
                    b.PaymentStatus = "failed"; // hoặc "cancelled" theo quy ước của bạn
                }

                if (expired.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "PendingBookingExpiryService loop error");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
