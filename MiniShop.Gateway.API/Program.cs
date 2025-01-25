using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiniShop.Gateway.API.Context;
using MiniShop.Gateway.API.Features.Auth;
using MiniShop.Gateway.API.Models;
using MiniShop.Gateway.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();

// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add YARP services
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "YourSuperSecretKeyWithAtLeast32Characters"))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Authenticated", policy =>
    {
        policy.RequireAuthenticatedUser(); // Yalnýzca giriþ yapmýþ kullanýcýlar
    });
});

builder.Services.AddSingleton<TokenService>();

var app = builder.Build();

app.UseCors(x => x.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod());

app.MapGet("/", () => "Hello World!");

// Register endpoint
app.MapPost("/api/auth/register", async (LoginRequest request, ApplicationDbContext dbContext, CancellationToken cancellationToken) =>
{
    // Kullanýcý adý kontrolü
    if (await dbContext.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
    {
        return Results.BadRequest("Kullanýcý zaten mevcut.");
    }

    var user = new User
    {
        Username = request.Username,
        Password = request.Password
    };

    await dbContext.Users.AddAsync(user, cancellationToken);

    await dbContext.SaveChangesAsync(cancellationToken);

    return Results.Ok("Kayýt baþarýlý.");
});

// Login endpoint
app.MapPost("/api/auth/login", async (LoginRequest request, ApplicationDbContext dbContext, IConfiguration configuration, CancellationToken cancellationToken, TokenService tokenService) =>
{
    // Kullanýcýyý bul
    var user = await dbContext.Users
        .SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);

    // Kullanýcý yoksa veya þifre yanlýþsa yetkisiz hata döndür
    if (user == null || user.Password != request.Password)
    {
        return Results.Unauthorized();
    }

    var token = tokenService.GenerateToken(user.Username);

    // Token'i döndür
    return Results.Ok(new { Token = token });
});

// Middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

// Migration'larý otomatik olarak uygula
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync(); // Migration'larý uygula
}

app.Run();
