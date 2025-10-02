using Microsoft.EntityFrameworkCore;
using Backend_Nghiencf.Data;
using Backend_Nghiencf.Services;
using Backend_Nghiencf.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend_Nghiencf.Options;
using System.Reflection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using DotNetEnv;
using Microsoft.Extensions.Hosting;
try
{
    // Tự động dò .env ở thư mục hiện tại; có thể chỉ rõ đường dẫn nếu muốn
    // Env.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));
    Env.Load();
}
catch { /* không có .env cũng không sao */ }

var builder = WebApplication.CreateBuilder(args);

// ---------- JWT ----------
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? throw new Exception("Jwt:SecretKey missing"));
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(int.Parse(port)));
builder.Services.Configure<HostOptions>(opt =>
{
    // .NET 8+: nếu BackgroundService throw ra ngoài, host sẽ KHÔNG dừng.
    opt.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});
builder.Configuration.AddEnvironmentVariables();
if (builder.Environment.IsDevelopment())
{
    try
    {
        var dotenv = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(dotenv))
        {
            foreach (var line in File.ReadAllLines(dotenv))
            {
                var idx = line.IndexOf('=');
                if (idx > 0)
                {
                    var k = line.Substring(0, idx).Trim();
                    var v = line.Substring(idx + 1).Trim();
                    Environment.SetEnvironmentVariable(k, v);
                }
            }
        }
    }
    catch { /* ignore */ }
}

builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    opt.TokenValidationParameters = new TokenValidationParameters
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

// ---------- Controllers / JSON ----------
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

// ---------- DbContext ----------
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("ConnectionStrings:DefaultConnection missing");
    builder.Services.AddOptions<TingeeOptions>()
    .Bind(builder.Configuration.GetSection("Tingee"))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "Missing Tingee:ClientId")
    .Validate(o => !string.IsNullOrWhiteSpace(o.SecretToken), "Missing Tingee:SecretToken")
    .Validate(o => o.Bank is not null && !string.IsNullOrWhiteSpace(o.Bank.AccountNumber), "Missing Tingee:Bank:AccountNumber")
    .ValidateOnStart();
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseMySql(connStr, ServerVersion.AutoDetect(connStr),
        mySql =>
        {
            mySql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
            mySql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
        });

    opt.EnableDetailedErrors();
    opt.EnableSensitiveDataLogging();
    opt.LogTo(Console.WriteLine, LogLevel.Information);
});

// ---------- DI ----------
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<IShowService, ShowService>();
builder.Services.AddScoped<ITicketTypeService, TicketTypeService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IBookingDevService, BookingDevService>();
builder.Services.AddHostedService<TicketBackfillService>();
builder.Services.AddHostedService<PendingBookingExpiryService>();
builder.Services.Configure<TingeeOptions>(builder.Configuration.GetSection("Tingee"));
builder.Services.AddHttpClient<ITingeeClient, TingeeClient>();

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // XML comments (chỉ include khi thực sự có file để tránh 500)
    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

    // (không bắt buộc) Security cho JWT -> không gây lỗi nếu không dùng
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "API", Version = "v1" });
    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập: Bearer {token}"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        [jwtScheme] = new List<string>()
    });
});

// ---------- CORS ----------
var allowedOrigins = new[] {
  "https://chamkhoanhkhac.com",
  "https://www.chamkhoanhkhac.com",
  "https://<your-vercel>.vercel.app" // nếu còn dùng
};

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
        c.RoutePrefix = "swagger";
    });
}
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
// Nếu bạn CHƯA cấu hình HTTPS endpoint trong launchSettings / Kestrel,
// tạm thời có thể comment dòng này khi test swagger để loại trừ redirect lỗi.
// app.UseHttpsRedirection();

app.UseStaticFiles();         // phục vụ /uploads/*
app.UseCors("AllowFrontend");

app.UseAuthentication();      // <<< THIẾU dòng này sẽ không kích hoạt JWT middleware
app.UseAuthorization();

app.MapControllers();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));
app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine("ASPNETCORE_URLS=" + Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
});
app.Run();
