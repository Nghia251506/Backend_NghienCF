using Microsoft.AspNetCore.Mvc;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Services;
using System.Threading;

namespace Backend_Nghiencf.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketTypeController : ControllerBase
    {
        private readonly ITicketTypeService _typeService;

        public TicketTypeController(ITicketTypeService typeService)
        {
            _typeService = typeService;
        }

        [HttpGet("by-show/{showId:int}")]
        public async Task<IActionResult> GetByShow([FromRoute] int showId, CancellationToken ct = default)
        {
            if (showId <= 0) return BadRequest("showId invalid.");
            var items = await _typeService.GetByShowAsync(showId, ct);
            return Ok(items);
        }

        [HttpGet("getall")]
        public async Task<IActionResult> GetAll(CancellationToken ct = default)
        {
            var items = await _typeService.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct = default)
        {
            var dto = await _typeService.GetTypeById(id);
            return dto == null ? NotFound() : Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TicketTypeCreateDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var created = await _typeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] TicketTypeUpdateDto dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var ok = await _typeService.UpdateAsync(id, dto);
            if (!ok) return NotFound();

            // Service trả bool → trả 204 cho đúng REST khi update thành công mà không trả content
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct = default)
        {
            var ok = await _typeService.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }
    }
}
