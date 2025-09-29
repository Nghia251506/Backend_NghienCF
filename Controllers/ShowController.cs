using Microsoft.AspNetCore.Mvc;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Services;

namespace Backend_Nghiencf.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class ShowController : ControllerBase
    {
        private readonly IShowService _showService;

        public ShowController(IShowService showService)
        {
            _showService = showService;
        }

        [HttpGet("getall")]
        public async Task<IActionResult> GetAll() => Ok(await _showService.GetAllAsync());

        [HttpGet("/{title}")]
        public async Task<IActionResult> GetByTitle(string title)
        {
            var show = await _showService.GetShowByTitleAsync(title);
            return show == null ? NotFound() : Ok(show);
        }
        [HttpPost]
        public async Task<ActionResult<ShowReadDto>> CreateShow([FromBody] ShowCreateDto dto)
        {
            var result = await _showService.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut("{title}")]
        public async Task<IActionResult> Update(string title, ShowUpdateDto dto)
        {
            var updated = await _showService.UpdateAsync(title, dto);

            if (updated == null)
                return NotFound();

            return Ok(updated); // hoặc NoContent() nếu bạn không cần trả dữ liệu về
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _showService.DeleteAsync(id);
            return ok ? NoContent() : NotFound();
        }

    }
}