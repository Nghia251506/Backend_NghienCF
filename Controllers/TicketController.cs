using Backend_Nghiencf.Dtos.Ticket;
using Backend_Nghiencf.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Nghiencf.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TicketController : ControllerBase
{
    private readonly ITicketService _service;
    private readonly ILogger<TicketController> _log;

    public TicketController(ITicketService service, ILogger<TicketController> log)
    {
        _service = service;
        _log = log;
    }

    // POST: /api/ticket/create
    [HttpPost("create")]
    public async Task<ActionResult<TicketReadDto>> Create([FromBody] TicketCreateDto dto, CancellationToken ct)
    {
        if (dto == null || dto.BookingId <= 0) return BadRequest("BookingId is required");

        try
        {
            var result = await _service.CreateAsync(dto.BookingId, dto.EventDate, ct);
            // 201 Created + location header
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            _log.LogWarning(ex, "Create ticket failed.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Create ticket error.");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    // GET: /api/ticket/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketReadDto>> GetById([FromRoute] int id, CancellationToken ct)
    {
        var t = await _service.GetByIdAsync(id, ct);
        if (t == null) return NotFound();
        return Ok(t);
    }

    // GET: /api/ticket/by-booking/{bookingId}
    [HttpGet("by-booking/{bookingId:int}")]
    public async Task<ActionResult<IReadOnlyList<TicketReadDto>>> GetByBooking([FromRoute] int bookingId, CancellationToken ct)
    {
        var list = await _service.GetByBookingAsync(bookingId, ct);
        return Ok(list);
    }

    // PATCH: /api/ticket/{id}/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus([FromRoute] int id, [FromBody] TicketStatusUpdateDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Status))
            return BadRequest("Status is required.");

        var ok = await _service.UpdateStatusAsync(id, dto.Status, ct);
        return ok ? NoContent() : NotFound();
    }
}
