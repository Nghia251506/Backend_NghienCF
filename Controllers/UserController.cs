using System.Security.Claims;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend_Nghiencf.Data;

namespace Backend_Nghiencf.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly AppDbContext _context;

        public UserController(IUserService userService, AppDbContext context)
        {
            _userService = userService;
            _context = context;
        }

        // Tạo user (tùy bạn có muốn bắt buộc admin hay không)
        // [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<UserReadDto>> CreateUser([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userService.CreateUserAsync(dto);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDto>> GetUserById(int id)
        {
            // TODO: implement trong service
            return NotFound("Chưa implement GetUserById trong service.");
        }

        /// <summary>
        /// Trả thông tin user hiện tại dựa trên JWT trong cookie 'atk'
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserReadDto>> Me()
        {
            // Lấy id từ các dạng claim phổ biến:
            var idClaim =
                User.FindFirst(ClaimTypes.NameIdentifier) ??
                User.FindFirst("sub") ??
                User.FindFirst("id");

            if (idClaim == null || !int.TryParse(idClaim.Value, out var id))
                return Unauthorized();

            var user = await _context.Users.FindAsync(id);
            if (user == null) return Unauthorized();

            return Ok(new UserReadDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Role = user.Role
            });
        }

        /// <summary>
        /// Đăng nhập: ghi JWT vào cookie HttpOnly "atk" + trả về thông tin user
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserReadDto>> Login([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var res = await _userService.LoginAsync(dto); // AuthResponse { Token, User } hoặc null
            if (res is null) return Unauthorized("Sai tên đăng nhập hoặc mật khẩu.");

            // Cookie options — đảm bảo hoạt động trên prod (HTTPS, cross-site) và dev (HTTP localhost)
            var cookieOpt = new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps || true,     // Railway dùng HTTPS: true; dev http: có thể đổi thành Request.IsHttps
                SameSite = SameSiteMode.None,         // cross-site XHR cần None
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                IsEssential = true,
                Path = "/"
                // KHÔNG set Domain trừ khi bạn biết chính xác cần dùng (đặt sai domain sẽ không lưu)
            };

            Response.Cookies.Append("atk", res.Token, cookieOpt);

            // Trả info user cho FE (FE không cần token nữa vì cookie đã có)
            return Ok(new UserReadDto
            {
                Id = res.User.Id,
                UserName = res.User.UserName,
                Role = res.User.Role
            });
        }

        /// <summary>
        /// Đăng xuất: xóa cookie "atk"
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Dùng cùng Path/SameSite/Secure như lúc set để chắc chắn xóa được
            Response.Cookies.Delete("atk", new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                Secure = Request.IsHttps || true
            });

            return NoContent();
        }
    }
}
