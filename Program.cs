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
using Microsoft.Extensions.FileProviders;
using Google.Analytics.Data.V1Beta;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
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
builder.Services.Configure<TingeeOptions>(builder.Configuration.GetSection("Tingee"));
builder.Services.AddMemoryCache();
builder.Services.Configure<GaOptions>(builder.Configuration.GetSection("Ga4"));

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
    "https://api.chamkhoanhkhac.com"
};

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
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
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
          ClockSkew = TimeSpan.Zero
      };
      options.Events = new JwtBearerEvents
      {
          OnMessageReceived = ctx => Task.CompletedTask,
          OnAuthenticationFailed = ctx =>
          {
              Console.WriteLine("JWT failed: " + ctx.Exception.Message);
              return Task.CompletedTask;
          }
      };
  });

builder.Services.AddAuthorization();

// ======== Controllers ========
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // tuỳ chọn: enum ra string, ignore null
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddSingleton<BetaAnalyticsDataClient>(sp =>
{
    var opt = sp.GetRequiredService<IOptions<GaOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opt.CredentialsPath))
    {
        if (!File.Exists(opt.CredentialsPath))
            throw new FileNotFoundException($"GA4 key not found at: {opt.CredentialsPath}");

        return new BetaAnalyticsDataClientBuilder
        {
            CredentialsPath = opt.CredentialsPath
        }.Build();
    }

    // Fallback: dùng ENV nếu không cấu hình CredentialsPath
    return new BetaAnalyticsDataClientBuilder().Build();
});

builder.Services.AddSingleton<IGa4Service, Ga4Service>();

// -------------- BUILD APP --------------
var app = builder.Build();

// ======== Global exception ========
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async ctx =>
    {
        var feat = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
        Console.WriteLine("[EX] PATH=" + feat?.Path);
        Console.WriteLine("[EX] " + feat?.Error);

        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        ctx.Response.ContentType = "application/json";
        await ctx.Response.WriteAsync("""{"error":"internal"}""");
    });
});

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

// ========== STATIC FILES ==========
app.UseHttpsRedirection();

// 1) đảm bảo có wwwroot
var webRoot = app.Environment.WebRootPath;
if (string.IsNullOrEmpty(webRoot))
{
    webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    Directory.CreateDirectory(webRoot);
    app.Environment.WebRootPath = webRoot;
}

// 2) đảm bảo có wwwroot/uploads
var uploadsPath = Path.Combine(webRoot, "uploads");
Directory.CreateDirectory(uploadsPath);

// 3) serve toàn bộ wwwroot: /css, /js, /uploads nếu có
app.UseStaticFiles();

// 4) (optional) serve riêng /uploads nếu muốn chắc chắn
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    // ⚠️ ở đây để /uploads chứ KHÔNG phải /api/uploads
    RequestPath = "/api/uploads"
});

// ======== PIPELINE ========
app.UseRouting();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

app.Run();

internal interface IOption
{
}