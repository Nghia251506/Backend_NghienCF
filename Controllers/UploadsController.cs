using Microsoft.AspNetCore.Mvc;

namespace Backend_Nghiencf.Controllers
{
    public class FileUploadDto
    {
        public IFormFile File { get; set; } = default!;
    }

    [ApiController]
    [Route("api/[controller]")]
    public class UploadsController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public UploadsController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Upload([FromForm] FileUploadDto dto)
        {
            var file = dto.File;
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Không có file được tải lên" });

            var wwwRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsPath = Path.Combine(wwwRoot, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(uploadsPath, fileName);

            await using (var stream = System.IO.File.Create(savePath))
            {
                await file.CopyToAsync(stream);
            }

            // 👇 trả về đường dẫn tương đối
            var host = Request.Host.Value;                   // ví dụ: api.tncom.xyz
            var publicUrl = $"https://{host}/uploads/{fileName}";

            return Ok(new
            {
                url = publicUrl,
                fileName,
                size = file.Length
            });
        }
    }
}
