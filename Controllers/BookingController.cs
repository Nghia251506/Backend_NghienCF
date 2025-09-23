using Microsoft.AspNetCore.Mvc;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Services;

namespace Backend_Nghiencf.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateBooking([FromBody] BookingDto dto)
        {
            var result = await _bookingService.CreateBookingAsync(dto);
            return Ok(result);
        }

        [HttpPost("confirm/{id}")]
        public async Task<IActionResult> ConfirmPayment(int id, [FromQuery] string transactionId)
        {
            var success = await _bookingService.ConfirmPaymentAsync(id, transactionId);
            if (!success) return NotFound();
            return Ok(new { message = "Thanh toán thành công" });
        }
    }
}
