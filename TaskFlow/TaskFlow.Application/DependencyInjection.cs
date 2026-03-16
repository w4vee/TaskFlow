using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Common.Behaviors;

namespace TaskFlow.Application;

/// <summary>
/// Extension method để đăng ký tất cả services của Application layer.
/// 
/// Tại sao dùng Extension Method trên IServiceCollection?
/// → Clean Architecture pattern: mỗi layer tự đăng ký services.
///   API layer chỉ cần gọi: builder.Services.AddApplication();
///   Không cần biết chi tiết bên trong có MediatR, AutoMapper, FluentValidation...
/// 
/// Assembly.GetExecutingAssembly():
/// → Scan toàn bộ DLL của Application layer để tìm:
///   - Tất cả MediatR Handlers (implement IRequestHandler)
///   - Tất cả AutoMapper Profiles (kế thừa Profile)
///   - Tất cả FluentValidation Validators (kế thừa AbstractValidator)
/// → Đăng ký vào DI container TỰ ĐỘNG, không cần register từng class.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Đăng ký MediatR + scan tất cả Handlers trong assembly
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Đăng ký AutoMapper + scan tất cả Profiles trong assembly
        services.AddAutoMapper(assembly);

        // Đăng ký FluentValidation + scan tất cả Validators trong assembly
        services.AddValidatorsFromAssembly(assembly);

        // Đăng ký Pipeline Behaviors (thứ tự QUAN TRỌNG!)
        // ValidationBehavior chạy TRƯỚC LoggingBehavior
        // → Nếu validation fail, không cần log
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        return services;
    }
}
