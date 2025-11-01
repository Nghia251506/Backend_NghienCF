using Microsoft.EntityFrameworkCore;
using Backend_Nghiencf.Models;

namespace Backend_Nghiencf.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Show> Shows { get; set; }
        public DbSet<TicketType> TicketTypes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<ThemeSetting> ThemeSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ThemeSetting>(e =>
            {
                e.ToTable("themesettings");
                e.HasKey(x => x.Id);
                e.Property(x => x.ShowId).HasColumnName("ShowId").IsRequired();
                e.Property(x => x.PrimaryColor).HasColumnName("PrimaryColor");
                e.Property(x => x.Accent).HasColumnName("Accent");
                e.Property(x => x.Background).HasColumnName("Background");
                e.Property(x => x.Surface).HasColumnName("Surface");
                e.Property(x => x.Text).HasColumnName("Text");
                e.Property(x => x.Muted).HasColumnName("Muted");
                e.Property(x => x.Navbar).HasColumnName("Navbar");
                e.Property(x => x.ButtonFrom).HasColumnName("ButtonFrom");
                e.Property(x => x.ButtonTo).HasColumnName("ButtonTo");
                e.Property(x => x.ScrollbarThumb).HasColumnName("ScrollbarThumb");
                e.Property(x => x.ScrollbarTrack).HasColumnName("ScrollbarTrack");
                e.Property(x => x.UpdatedAt).HasColumnName("UpdatedAt");
                e.Property(x => x.CreatedAt).HasColumnName("CreatedAt");
            });

            modelBuilder.Entity<Show>(e =>
            {
                e.ToTable("shows");
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasColumnName("title").IsRequired();
                e.Property(x => x.Description).HasColumnName("description");
                e.Property(x => x.Date).HasColumnName("date");
                e.Property(x => x.Location).HasColumnName("location");
                e.Property(x => x.BannerUrl).HasColumnName("banner_url");
                e.Property(x => x.Capacity).HasColumnName("capacity");
                e.Property(x => x.Slogan).HasColumnName("slogan");
                e.Property(x => x.IsDefault).HasColumnName("isDefault");
                e.Property(x => x.DeleteStatus).HasColumnName("deleteStatus");
            });
            modelBuilder.Entity<User>(e =>
            {
                e.ToTable("admin_users");
                e.HasKey(x => x.Id);
                e.Property(x => x.UserName).HasColumnName("username");
                e.Property(x => x.PassWord).HasColumnName("password");
                e.Property(x => x.Email).HasColumnName("email");
                e.Property(x => x.Role).HasColumnName("role");
            });

            modelBuilder.Entity<Ticket>(e =>
            {
                e.ToTable("tickets");
                e.HasKey(x => x.Id);
                e.Property(x => x.BookingId).HasColumnName("booking_id");
                e.Property(x => x.TicketCode).HasColumnName("ticket_code");
                e.Property(x => x.Status).HasColumnName("status");
                e.Property(x => x.IssuedAt).HasColumnName("issued_at");
                e.HasOne(x => x.Booking)
                    .WithMany()
                    .HasForeignKey(x => x.BookingId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("tickets_ibfk_1");
            });

            modelBuilder.Entity<Setting>(e =>
            {
                e.ToTable("settings");
                e.HasKey(x => x.Id);
                e.Property(x => x.SettingKey).HasColumnName("setting_key").IsRequired();
                e.Property(x => x.SettingValue).HasColumnName("setting_value");
                e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<Booking>(e =>
                {
                    e.ToTable("bookings");
                    e.HasKey(x => x.Id);

                    // 🔴 BẮT BUỘC: map cột id và báo cho EF biết là identity
                    e.Property(x => x.Id)
                        .HasColumnName("id")
                        .ValueGeneratedOnAdd()
                        .UseMySqlIdentityColumn(); // Pomelo
                    e.Property(x => x.BookingCode).HasColumnName("PaymentRef");

                    e.Property(x => x.ShowId).HasColumnName("show_id");
                    e.Property(x => x.TicketTypeId).HasColumnName("ticket_type_id");
                    e.Property(x => x.CustomerName).HasColumnName("customer_name");
                    e.Property(x => x.Phone).HasColumnName("phone");
                    e.Property(x => x.Quantity).HasColumnName("quantity");

                    e.Property(x => x.TotalAmount)
                        .HasColumnName("total_amount")
                        .HasColumnType("decimal(15,2)");

                    e.Property(x => x.PaymentStatus).HasColumnName("payment_status");
                    e.Property(x => x.PaymentTime).HasColumnName("payment_time");

                    e.Property(x => x.CreatedAt)
                        .HasColumnName("created_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    e.HasOne(x => x.TicketType)
                        .WithMany()
                        .HasForeignKey(x => x.TicketTypeId)
                        .OnDelete(DeleteBehavior.Restrict);

                    e.HasOne(x => x.Show)
                        .WithMany()
                        .HasForeignKey(x => x.ShowId)
                        .OnDelete(DeleteBehavior.Restrict);
                });

            modelBuilder.Entity<TicketType>(e =>
            {
                e.ToTable("ticket_types");
                e.HasKey(x => x.Id);
                e.Property(x => x.ShowId).HasColumnName("show_id");
                e.Property(x => x.Name).HasColumnName("name");
                e.Property(x => x.Color).HasColumnName("color");
                e.Property(x => x.Price).HasColumnName("price");
                e.Property(x => x.Description).HasColumnName("description");
                e.Property(x => x.TotalQuantity).HasColumnName("total_quantity");
                e.Property(x => x.RemainingQuantity).HasColumnName("remaining_quantity");

                e.HasOne(x => x.Show)
                    .WithMany() // nếu Show có ICollection<TicketType> đổi lại
                    .HasForeignKey(x => x.ShowId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
