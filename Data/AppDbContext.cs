using Microsoft.EntityFrameworkCore;
using ProjectName.Models;

namespace ProjectName.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<Show> Shows { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<Setting> Settings { get; set; }
    }
}
