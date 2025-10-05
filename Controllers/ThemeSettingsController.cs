// Controllers/ThemeSettingsController.cs
using Backend_Nghiencf.Dtos;
using Backend_Nghiencf.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Nghiencf.Controllers;

[ApiController]
[Route("api/theme-settings")]
public sealed class ThemeSettingsController : ControllerBase
{
    private readonly IThemeService _svc;
    private string? CurrentUser => User?.Identity?.Name ?? "system";

    public ThemeSettingsController(IThemeService svc) => _svc = svc;

    /// GET /api/theme-settings
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ThemeSettingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _svc.GetAllAsync(ct));

    /// GET /api/theme-settings/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ThemeSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => (await _svc.GetByIdAsync(id, ct)) is { } dto ? Ok(dto) : NotFound();

    /// GET /api/theme-settings/active?showId=123
    [HttpGet("active")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ThemeSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetActive([FromQuery] int? showId, CancellationToken ct)
        => (await _svc.GetActiveAsync(showId, ct)) is { } dto ? Ok(dto) : NoContent();

    /// POST /api/theme-settings
    [HttpPost]
    [ProducesResponseType(typeof(ThemeSettingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] ThemeCreateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var created = await _svc.CreateAsync(dto, CurrentUser, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// PUT /api/theme-settings/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ThemeSettingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ThemeUpdateDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        try
        {
            var updated = await _svc.UpdateAsync(id, dto, CurrentUser, ct);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// DELETE /api/theme-settings/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
        => await _svc.DeleteAsync(id, ct) ? NoContent() : NotFound();
}
