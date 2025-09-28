using Microsoft.EntityFrameworkCore;
using Backend_Nghiencf.Data;
using Backend_Nghiencf.Services;
using Backend_Nghiencf.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend_Nghiencf.Models;
using Microsoft.AspNetCore.Cors;
using Backend_Nghiencf.Options;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);
var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseMySql(
        connStr,
        ServerVersion.AutoDetect(connStr),           // 👈 tự dò version MySQL
        mySql =>
        {
            mySql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            mySql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
        });

    // 👇 bật log để thấy INSERT/UPDATE thực tế EF chạy
    opt.EnableDetailedErrors();
    opt.EnableSensitiveDataLogging();
    opt.LogTo(Console.WriteLine, LogLevel.Information);
});
// Bind options từ appsettings.json (section "Tingee")
builder.Services.Configure<TingeeOptions>(builder.Configuration.GetSection("Tingee"));

// Đăng ký HttpClient cho TingeeClient
builder.Services.AddHttpClient<ITingeeClient, TingeeClient>();
// builder.Services.AddSingleton<ITingeeClient>(new FakeTingeeClient()); // trả về URL giả
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<IShowService, ShowService>();
builder.Services.AddScoped<ITicketTypeService, TicketTypeService>();
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddHostedService<PendingBookingExpiryService>();
builder.Services.Configure<TingeeOptions>(
    builder.Configuration.GetSection("Tingee"));

builder.Services.AddHttpClient<ITingeeClient, TingeeClient>();


Console.WriteLine($"Tingee:ClientId = '{builder.Configuration["Tingee:ClientId"]}'");
Console.WriteLine($"Tingee:SecretToken = '{builder.Configuration["Tingee:SecretToken"]?.Substring(0,4)}***'");
Console.WriteLine($"[CONF] Tingee:Bank:BankName = '{builder.Configuration["Tingee:Bank:BankName"]}'");
Console.WriteLine($"[CONF] Tingee:Bank:AccountNumber = '{builder.Configuration["Tingee:Bank:AccountNumber"]}'");




builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
