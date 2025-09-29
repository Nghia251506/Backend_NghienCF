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
        [RequestSizeLimit(20_000_000)] // giới hạn 20MB
        public async Task<IActionResult> Upload([FromForm] FileUploadDto dto)
        {
            var file = dto.File;

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "❌ Không có file được tải lên" });

            // Đảm bảo thư mục /wwwroot/uploads tồn tại
            var wwwRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsPath = Path.Combine(wwwRoot, "uploads");
            Directory.CreateDirectory(uploadsPath);

            // Tạo tên file duy nhất
            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(uploadsPath, fileName);

            await using (var stream = System.IO.File.Create(savePath))
            {
                await file.CopyToAsync(stream);
            }

            // Trả về URL công khai (ví dụ: http://localhost:5135/uploads/abc.jpg)
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var publicUrl = $"{baseUrl}/uploads/{fileName}";

            return Ok(new
            {
                url = publicUrl,
                fileName = fileName,
                size = file.Length
            });
        }
    }
}
