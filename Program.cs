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
using DotNetEnv;

try
{
    Env.Load(); // đọc file .env nếu có
}
catch { }

var builder = WebApplication.CreateBuilder(args);

// ======== JWT ========
var jwtSecret = builder.Configuration["Jwt:SecretKey"] ?? throw new Exception("Missing Jwt:SecretKey");
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(int.Parse(port)));

// ======== DB ========
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new Exception("ConnectionStrings:DefaultConnection missing");

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseMySql(connStr, ServerVersion.AutoDetect(connStr), mySql =>
    {
        mySql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        mySql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    });

    opt.EnableDetailedErrors();
    opt.EnableSensitiveDataLogging();
    opt.LogTo(Console.WriteLine, LogLevel.Information);
});

// ======== DI ========
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISettingService, SettingService>();
builder.Services.AddScoped<IShowService, ShowService>();
builder.Services.AddScoped<ITicketTypeService, TicketTypeService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<IBookingDevService, BookingDevService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddHostedService<TicketBackfillService>();
builder.Services.AddHostedService<PendingBookingExpiryService>();
builder.Services.AddHttpClient<ITingeeClient, TingeeClient>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// ======== Swagger ========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlName);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
    }

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

// ======== CORS ========
var allowedOrigins = new[]
{
    "https://chamkhoanhkhac.com",
    "https://www.chamkhoanhkhac.com",
    "http://localhost:5173",
    "http://127.0.0.1:5173",
    "https://frontend-nghien-cf.vercel.app",
};

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
    );
});

// ======== Auth ========
var secret = builder.Configuration["Jwt:SecretKey"] 
             ?? throw new Exception("Jwt:SecretKey missing");
builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
      options.TokenValidationParameters = new TokenValidationParameters
      {
          ValidateIssuer = false,
          ValidateAudience = false,
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(
              Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)
          ),
          ClockSkew = TimeSpan.Zero
      };

      options.Events = new JwtBearerEvents
      {
          OnMessageReceived = ctx =>
          {
              if (ctx.Request.Cookies.TryGetValue("atk", out var token))
                  ctx.Token = token;          // 👈 QUAN TRỌNG: gán token từ cookie
              return Task.CompletedTask;
          },
          OnAuthenticationFailed = ctx =>
          {
              Console.WriteLine("[JWT] failed: " + ctx.Exception.Message);
              return Task.CompletedTask;
          }
      };
  });

builder.Services.AddAuthorization();

// ======== Controllers ========
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// ======== Dev Swagger ========
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

// ======== DB Migrate ========
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ======== Middlewares ========
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowFrontend");      // ⚠️ phải đứng TRƯỚC auth
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.Run();
