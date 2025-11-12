using Google.Analytics.Data.V1Beta;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

public interface IGa4Service
{
    Task<IEnumerable<DailyRow>> GetDailyAsync(string start, string end);
    Task<IEnumerable<TopRow>> GetTopPagesAsync(string start, string end, int limit = 20);
    Task<IEnumerable<TopRow>> GetReferrersAsync(string start, string end, int limit = 20);
    Task<IEnumerable<PieRow>> GetDevicesAsync(string start, string end);
}

public record DailyRow(string date, int users, int pageViews);
public record TopRow(string label, int value);
public record PieRow(string label, int value);

public class Ga4Service : IGa4Service
{
    private readonly BetaAnalyticsDataClient _client;
    private readonly string _property;
    private readonly IMemoryCache _cache;

    public Ga4Service(BetaAnalyticsDataClient client, IOptions<GaOptions> opt, IMemoryCache cache)
    {
        _client = client;                     
        _property = opt.Value.Property;
        _cache = cache;
    }
    public async Task<IEnumerable<DailyRow>> GetDailyAsync(string start, string end)
    {
        var cacheKey = $"ga_daily_{start}_{end}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<DailyRow>? got)) return got!;

        var req = new RunReportRequest
        {
            Property = _property,
            DateRanges = { new DateRange { StartDate = start, EndDate = end } },
            Dimensions = { new Dimension { Name = "date" } },
            Metrics =
            {
                new Metric { Name = "activeUsers" },
                new Metric { Name = "screenPageViews" }
            }
        };
        var res = await _client.RunReportAsync(req);
        var data = res.Rows.Select(r => new DailyRow(
            date: r.DimensionValues[0].Value,                  // yyyyMMdd
            users: int.Parse(r.MetricValues[0].Value),
            pageViews: int.Parse(r.MetricValues[1].Value)
        )).ToList();

        _cache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
        return data;
    }

    public async Task<IEnumerable<TopRow>> GetTopPagesAsync(string start, string end, int limit = 20)
    {
        var cacheKey = $"ga_top_pages_{start}_{end}_{limit}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<TopRow>? got)) return got!;

        var req = new RunReportRequest
        {
            Property = _property,
            DateRanges = { new DateRange { StartDate = start, EndDate = end } },
            Dimensions = { new Dimension { Name = "pagePath" } },
            Metrics = { new Metric { Name = "screenPageViews" } },
            OrderBys = { new OrderBy { Metric = new OrderBy.Types.MetricOrderBy { MetricName = "screenPageViews" }, Desc = true } },
            Limit = limit
        };
        var res = await _client.RunReportAsync(req);
        var data = res.Rows.Select(r => new TopRow(
            label: r.DimensionValues[0].Value,
            value: int.Parse(r.MetricValues[0].Value)
        )).ToList();

        _cache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
        return data;
    }

    public async Task<IEnumerable<TopRow>> GetReferrersAsync(string start, string end, int limit = 20)
    {
        var cacheKey = $"ga_ref_{start}_{end}_{limit}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<TopRow>? got)) return got!;

        var req = new RunReportRequest
        {
            Property = _property,
            DateRanges = { new DateRange { StartDate = start, EndDate = end } },
            Dimensions = { new Dimension { Name = "sessionSource" } }, // hoặc "fullReferrer"
            Metrics = { new Metric { Name = "sessions" } },
            OrderBys = { new OrderBy { Metric = new OrderBy.Types.MetricOrderBy { MetricName = "sessions" }, Desc = true } },
            Limit = limit
        };
        var res = await _client.RunReportAsync(req);
        var data = res.Rows.Select(r => new TopRow(
            label: string.IsNullOrEmpty(r.DimensionValues[0].Value) ? "(direct)" : r.DimensionValues[0].Value,
            value: int.Parse(r.MetricValues[0].Value)
        )).ToList();

        _cache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
        return data;
    }

    public async Task<IEnumerable<PieRow>> GetDevicesAsync(string start, string end)
    {
        var cacheKey = $"ga_dev_{start}_{end}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<PieRow>? got)) return got!;

        var req = new RunReportRequest
        {
            Property = _property,
            DateRanges = { new DateRange { StartDate = start, EndDate = end } },
            Dimensions = { new Dimension { Name = "deviceCategory" } },
            Metrics = { new Metric { Name = "activeUsers" } }
        };
        var res = await _client.RunReportAsync(req);
        var data = res.Rows.Select(r => new PieRow(
            label: r.DimensionValues[0].Value,  // desktop / mobile / tablet
            value: int.Parse(r.MetricValues[0].Value)
        )).ToList();

        _cache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
        return data;
    }
}

public sealed class GaOptions
{
    public string Property { get; set; } = "";
    public string? CredentialsPath { get; set; }
}