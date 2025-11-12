using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin/ga")]
public class GaController : ControllerBase
{
    private readonly IGa4Service _svc;
    public GaController(IGa4Service svc) { _svc = svc; }

    // start/end có thể là "2025-11-01" hoặc "30daysAgo" / "today"
    [HttpGet("daily")]
    public async Task<IActionResult> Daily([FromQuery] string start = "30daysAgo", [FromQuery] string end = "today")
        => Ok(await _svc.GetDailyAsync(start, end));

    [HttpGet("top-pages")]
    public async Task<IActionResult> TopPages([FromQuery] string start = "30daysAgo", [FromQuery] string end = "today", [FromQuery] int limit = 20)
        => Ok(await _svc.GetTopPagesAsync(start, end, limit));

    [HttpGet("referrers")]
    public async Task<IActionResult> Referrers([FromQuery] string start = "30daysAgo", [FromQuery] string end = "today", [FromQuery] int limit = 20)
        => Ok(await _svc.GetReferrersAsync(start, end, limit));

    [HttpGet("devices")]
    public async Task<IActionResult> Devices([FromQuery] string start = "30daysAgo", [FromQuery] string end = "today")
        => Ok(await _svc.GetDevicesAsync(start, end));
}
