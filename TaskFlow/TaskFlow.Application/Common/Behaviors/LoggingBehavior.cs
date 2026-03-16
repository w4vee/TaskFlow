using MediatR;
using Microsoft.Extensions.Logging;

namespace TaskFlow.Application.Common.Behaviors;

/// <summary>
/// Pipeline Behavior để log mọi request vào/ra.
/// 
/// Rất hữu ích để debug: biết request nào đang chạy, mất bao lâu.
/// 
/// Log output ví dụ:
///   [INFO] Handling RegisterCommand
///   [INFO] Handled RegisterCommand in 45ms
/// 
/// ILogger: interface logging built-in của .NET, 
/// output ra Console, File, Application Insights... tùy cấu hình.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        _logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}
