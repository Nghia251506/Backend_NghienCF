using Microsoft.EntityFrameworkCore;
using Backend_Nghiencf.Data;
using Backend_Nghiencf.DTOs;
using Backend_Nghiencf.Models;

namespace Backend_Nghiencf.Services
{
    public class BookingService : IBookingService
    {
        private readonly AppDbContext _context;

        public BookingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Booking>> GetAllSync()
        {
            return await _context.Bookings
                    .Select(e => new Booking
                    {
                        Id = e.Id,
                        ShowId = e.ShowId,
                        Show = e.Show,
                        TicketTypeId = e.TicketTypeId,
                        TicketType = e.TicketType,
                        CustomerName = e.CustomerName,
                        Phone = e.Phone,
                        Quantity = e.Quantity,
                        TotalAmount = e.TotalAmount,
                        PaymentStatus = e.PaymentStatus,
                        PaymentTime = e.PaymentTime
                    }).ToListAsync();
        }
        public async Task<BookingResponseDto> CreateBookingAsync(BookingDto dto)
        {
            var ticketType = await _context.TicketTypes.FindAsync(dto.TicketTypeId);
            if (ticketType == null || ticketType.RemainingQuantity < dto.Quantity)
                throw new Exception("Không đủ số lượng vé");

            var booking = new Booking
            {
                ShowId = ticketType.ShowId,
                TicketTypeId = dto.TicketTypeId,
                CustomerName = dto.CustomerName,
                Phone = dto.Phone,
                Quantity = dto.Quantity,
                TotalAmount = ticketType.Price * dto.Quantity,
                PaymentStatus = "pending"
            };

            _context.Bookings.Add(booking);
            ticketType.RemainingQuantity -= dto.Quantity;

            await _context.SaveChangesAsync();

            // Ở đây bạn call API Tingee để lấy QR
            string qrUrl = $"https://tingee.fake/qr/{booking.Id}";

            return new BookingResponseDto
            {
                BookingId = booking.Id,
                TotalAmount = booking.TotalAmount,
                PaymentQrUrl = qrUrl
            };
        }

        public async Task<bool> ConfirmPaymentAsync(int bookingId, string transactionId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return false;

            booking.PaymentStatus = "paid";
            booking.PaymentTime = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
