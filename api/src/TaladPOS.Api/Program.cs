using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaladPOS.Api.Middleware;
using TaladPOS.Application.Auth;
using TaladPOS.Application.Products;
using TaladPOS.Application.SalesOrders;
using TaladPOS.Domain.Entities;
using TaladPOS.Infrastructure.Auth;
using TaladPOS.Infrastructure.Persistence;
using TaladPOS.Infrastructure.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<TaladPOSDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"Missing '{JwtOptions.SectionName}' configuration section.");
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<LoginUseCase>();

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISalesOrderRepository, SalesOrderRepository>();
builder.Services.AddScoped<SearchProductsQuery>();
builder.Services.AddScoped<CheckoutUseCase>();
builder.Services.AddScoped<VoidSalesOrderUseCase>();
builder.Services.AddScoped<PricingPreviewQuery>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

// Admin is a superset of Cashier (FR-034); the CashierOrAdmin policy accepts either role.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole(nameof(StaffRole.Admin)));
    options.AddPolicy("CashierOrAdmin", policy => policy.RequireRole(nameof(StaffRole.Cashier), nameof(StaffRole.Admin)));
});

// web/ (Next.js dev server) calls this API cross-origin (Constitution Principle I — REST-only).
const string WebClientCorsPolicy = "WebClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebClientCorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(WebClientCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
