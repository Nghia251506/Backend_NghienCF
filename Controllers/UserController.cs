using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend_Nghiencf.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
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

        // POST: api/User/login
        [HttpPost("login")]
        public async Task<ActionResult<UserReadDto>> Login([FromBody] UserLoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userService.LoginAsync(dto);
            if (user == null) return Unauthorized("Sai tên đăng nhập hoặc mật khẩu.");

            return Ok(user);
        }
    }
}
