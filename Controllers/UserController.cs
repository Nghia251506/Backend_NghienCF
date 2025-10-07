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

        // POST: api/User
        [HttpPost]
        public async Task<ActionResult<UserReadDto>> CreateUser([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.CreateUserAsync(dto);
            return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
        }

        // GET: api/User/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserReadDto>> GetUserById(int id)
        {
            // hiện tại service bạn chưa có GetUserById
            // mình viết tạm return NotFound() cho bạn
            return NotFound("Chưa implement GetUserById trong service.");
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserReadDto>> Me()
        {
            var userId = User.FindFirst("sub")?.Value // hoặc ClaimTypes.NameIdentifier
                       ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var id = int.Parse(userId);
            var user = await _context.Users.FindAsync(id);
            if (user == null) return Unauthorized();

            return Ok(new UserReadDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Role = user.Role
            });
        }

        // POST: api/User/login
        [HttpPost("login")]
        public async Task<ActionResult<UserReadDto>> Login([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var res = await _userService.LoginAsync(dto); // => AuthResponse { Token, User } hoặc null
            if (res is null) return Unauthorized("Sai tên đăng nhập hoặc mật khẩu.");

            // Ghi JWT vào cookie HttpOnly
            var cookieOpt = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,              // nhớ dùng HTTPS ở production
                SameSite = SameSiteMode.None,  // hoặc Strict nếu không cần cross-site
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                IsEssential = true,
                Path = "/"
            };
            Response.Cookies.Append("atk", res.Token, cookieOpt);

            // Trả về info user (không cần trả token cho FE nữa)
            return Ok(res.User);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("atk", new CookieOptions { Path = "/" });
            return NoContent();
        }
    }
}
