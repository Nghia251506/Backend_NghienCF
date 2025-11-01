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

            // xác định webroot
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            Directory.CreateDirectory(webRoot);

            var uploadsPath = Path.Combine(webRoot, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var ext = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var savePath = Path.Combine(uploadsPath, fileName);

            await using (var stream = System.IO.File.Create(savePath))
            {
                await file.CopyToAsync(stream);
            }

            // ⚠️ quan trọng: proxy của bạn chỉ mở /api/* nên mình trả về /api/uploads/...
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var publicUrl = $"{baseUrl}/api/uploads/{fileName}";

            return Ok(new
            {
                url = publicUrl,
                fileName,
                size = file.Length
            });
        }

        // optional: để bạn xem đang có file nào
        [HttpGet("list")]
        public IActionResult List()
        {
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsPath = Path.Combine(webRoot, "uploads");
            if (!Directory.Exists(uploadsPath))
                return Ok(Array.Empty<string>());

            var files = Directory.GetFiles(uploadsPath)
                .Select(Path.GetFileName)
                .Select(name => new {
                    name,
                    url = $"{Request.Scheme}://{Request.Host}/api/uploads/{name}"
                });

            return Ok(files);
        }
    }
}
