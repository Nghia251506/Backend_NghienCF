using Microsoft.AspNetCore.Mvc;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Services;

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
        public async Task<IActionResult> GetByShow([FromRoute] int showId, CancellationToken ct)
        {
            if (showId <= 0) return BadRequest("showId invalid.");
            var items = await _typeService.GetByShowAsync(showId, ct);
            return Ok(items);
        }

        [HttpGet("getall")]
        public async Task<IActionResult> GetAll() => Ok(await _typeService.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var show = await _typeService.GetTypeById(id);
            return show == null ? NotFound() : Ok(show);
        }

        [HttpPost]
        public async Task<IActionResult> Create(TicketTypeCreateDto dto)
        {
            var emp = await _typeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = emp.Id }, emp);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, TicketTypeUpdateDto dto)
        {
            var updated = await _typeService.UpdateAsync(id, dto);

            if (updated == null)
                return NotFound();

            return Ok(updated); // hoặc NoContent() nếu bạn không cần trả dữ liệu về
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _typeService.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

    }
}