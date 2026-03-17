using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Application.Interfaces;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;
using TaskFlow.Infrastructure.Repositories;
using TaskFlow.Infrastructure.Services;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace TaskFlow.Infrastructure;

/// <summary>
/// DependencyInjection - extension method để register tất cả Infrastructure services.
/// 
/// Mỗi layer có 1 file DependencyInjection.cs riêng:
/// - Application: AddApplication() → MediatR, AutoMapper, FluentValidation, Behaviors
/// - Infrastructure: AddInfrastructure() → DbContext, Repositories, UnitOfWork, Auth services
/// 
/// API layer (Program.cs) chỉ cần:
///   builder.Services.AddApplication();
///   builder.Services.AddInfrastructure(builder.Configuration);
/// → Gọn gàng, mỗi layer quản lý DI của mình.
/// 
/// Service Lifetimes (QUAN TRỌNG cho phỏng vấn):
/// 
/// Singleton: 1 instance suốt đời app
///   → Dùng cho: stateless services, caching, configuration
///   → KHÔNG dùng cho: DbContext (vì track changes per request)
/// 
/// Scoped: 1 instance per HTTP request
///   → Dùng cho: DbContext, UnitOfWork, Repositories
///   → Lý do: mỗi request cần track changes riêng, SaveChanges riêng
///   → Request A thay đổi User X, Request B thay đổi User Y → không conflict
/// 
/// Transient: tạo mới mỗi lần inject
///   → Dùng cho: stateless, lightweight services (PasswordHasher)
///   → Mỗi lần cần → tạo mới → không share state
/// 
/// QUY TẮC: Scoped service KHÔNG được inject vào Singleton service!
///   Vì Singleton sống mãi → Scoped bên trong cũng sống mãi → memory leak + wrong data.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ===== 1. DATABASE (DbContext) =====
        // AddDbContext<AppDbContext> register với Scoped lifetime (default)
        // UseSqlServer: dùng SQL Server provider + connection string từ config
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                // Chỉ định assembly chứa Migrations
                // Migrations sẽ được tạo trong Infrastructure project
                b => b.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        // ===== 2. REPOSITORIES (Scoped) =====
        // Scoped vì repositories dùng DbContext (cũng Scoped)
        // Khi inject IUserRepository → DI tạo UserRepository(AppDbContext)
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskBoardRepository, TaskBoardRepository>();
        services.AddScoped<ITaskItemRepository, TaskItemRepository>();

        // ===== 3. UNIT OF WORK (Scoped) =====
        // Scoped vì UnitOfWork quản lý DbContext + Repositories cho 1 request
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ===== 4. APPLICATION SERVICES =====
        // PasswordHasher: Scoped (hoặc Transient đều OK vì stateless)
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        // TokenService: Scoped (cần IConfiguration - Singleton, nên Scoped OK)
        services.AddScoped<ITokenService, TokenService>();

        // ===== 5. JWT AUTHENTICATION =====
        // Cấu hình ASP.NET Core Authentication middleware
        // Khi request đến → middleware đọc header "Authorization: Bearer <token>"
        // → validate token theo rules bên dưới → nếu OK thì set HttpContext.User
        services.AddAuthentication(options =>
        {
            // DefaultAuthenticateScheme: scheme mặc định khi authenticate
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            // DefaultChallengeScheme: scheme khi [Authorize] fail → return 401
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                // Validate Issuer: kiểm tra token do server này phát hành
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],

                // Validate Audience: kiểm tra token dành cho app này
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],

                // Validate Signing Key: kiểm tra chữ ký không bị giả mạo
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),

                // Validate Lifetime: kiểm tra token chưa hết hạn
                ValidateLifetime = true,

                // ClockSkew: cho phép lệch thời gian giữa server 0-5 giây
                // Default là 5 phút (quá rộng) → set về 0 cho strict
                ClockSkew = TimeSpan.Zero
            };
        });

        // ===== 6. REDIS CACHING =====
        // Register Redis cache service
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "TaskFlow:";
        });

        return services;
    }
}
