using Microsoft.EntityFrameworkCore;
using Backend_Nghiencf.Data;

public class PendingBookingExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingBookingExpiryService> _logger;

    public PendingBookingExpiryService(IServiceScopeFactory scopeFactory, ILogger<PendingBookingExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var cutoff = DateTime.UtcNow.AddMinutes(-15);  // hay theo VN time nếu bạn lưu local
                const int take = 500;

                var sql = @"
                            UPDATE bookings
                            SET payment_status = 'failed'
                            WHERE payment_status = 'pending'
                            AND created_at < {0}
                            LIMIT {1};"; // MySQL cho phép LIMIT trong UPDATE

                // ❌ SAI: truyền stoppingToken lẫn vào tham số SQL
                // await db.Database.ExecuteSqlRawAsync(sql, cutoff, take, stoppingToken);

                // ✅ ĐÚNG: tham số SQL trong mảng object[], token là tham số cuối
                var affected = await db.Database.ExecuteSqlRawAsync(
                    sql,
                    new object[] { cutoff, take },
                    stoppingToken
                );

                _logger.LogInformation("Expired {count} pending bookings older than {cutoff}", affected, cutoff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expire pending bookings failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
