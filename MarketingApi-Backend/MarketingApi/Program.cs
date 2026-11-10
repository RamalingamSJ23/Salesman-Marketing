using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using MarketingApi.Common;
using MarketingApi.Data;
using MarketingApi.DTOs.MarketingTeam;
using MarketingApi.DTOs.Salesman;
using MarketingApi.Models.Entities;
using MarketingApi.Options;
using MarketingApi.Repository.Implementations;
using MarketingApi.Repository.Interfaces;
using MarketingApi.Service.Implementations;
using MarketingApi.Service.Interfaces;
using MarketingApi.Validation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/marketing-api-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var isEfDesignTime = EF.IsDesignTime;

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (!isEfDesignTime && string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException("JWT key is missing. Set Jwt:Key in user-secrets or environment variables.");
}

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();

builder.Services.AddDbContext<SalesMarketingDbContext>((sp, options) =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .UseSnakeCaseNamingConvention()
           .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

builder.Services.AddScoped<IUsersRepository, UsersRepository>();
builder.Services.AddScoped<ISalesmanRepository, SalesmanRepository>();
builder.Services.AddScoped<IMarketingTeamRepository, MarketingTeamRepository>();

builder.Services.AddScoped<IValidator<CreateSalesmanDto>, CreateSalesmanDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateSalesmanDto>, UpdateSalesmanDtoValidator>();
builder.Services.AddScoped<IValidator<CreateMarketingTeamDto>, CreateMarketingTeamDtoValidator>();
builder.Services.AddScoped<IValidator<UpdateMarketingTeamDto>, UpdateMarketingTeamDtoValidator>();

builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<ISalesmanService, SalesmanService>();
builder.Services.AddScoped<IMarketingTeamService, MarketingTeamService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

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
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            string.IsNullOrWhiteSpace(jwtOptions.Key) ? "design-time-placeholder-key-not-used" : jwtOptions.Key)),
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(AppRoles.SalesmanPolicy, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.SupervisorMaster));

    options.AddPolicy(AppRoles.MarketingPolicy, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.MarketingMaster));

    options.AddPolicy(AppRoles.AnyMasterPolicy, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.SupervisorMaster, AppRoles.MarketingMaster));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// One-time CLI utility to hash legacy plaintext passwords in the users table (C3)
if (args.Contains("--hash-passwords", StringComparer.OrdinalIgnoreCase))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<SalesMarketingDbContext>();
    var hasher = new PasswordHasher<User>();
    var users = await db.Users.ToListAsync();
    var updatedCount = 0;

    foreach (var user in users)
    {
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            continue;
        }

        // ASP.NET Identity v3 PBKDF2 hashes always start with "AQAAAA"
        if (!user.PasswordHash.StartsWith("AQAAAA", StringComparison.Ordinal))
        {
            user.PasswordHash = hasher.HashPassword(user, user.PasswordHash);
            updatedCount++;
        }
    }

    if (updatedCount > 0)
    {
        await db.SaveChangesAsync();
    }

    Log.Information("Password hashing script completed. Updated {Count} user(s).", updatedCount);
    return;
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sales & Marketing API v1");
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
