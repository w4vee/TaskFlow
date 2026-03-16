using System.Net;
using System.Text.Json;
using TaskFlow.Application.Common.Exceptions;

namespace TaskFlow.API.Middleware;

/// <summary>
/// Global Exception Handling Middleware.
/// 
/// Middleware là gì?
/// Là "tầng trung gian" mà MỌI request phải đi qua trước khi đến Controller.
/// Giống như bảo vệ ở cổng công ty -- ai vào cũng phải qua bảo vệ.
/// 
/// Flow:
///   Request → ExceptionMiddleware → Authentication → Controller → Handler
///                    ↑                                              ↓
///                    └───── Nếu Handler throw exception ───────────┘
///                          Middleware catch → return JSON error
/// 
/// Tại sao cần Global Exception Handling?
/// - Không có: Handler throw NotFoundException → server trả 500 Internal Server Error (sai!)
/// - Có middleware: NotFoundException → middleware catch → trả 404 Not Found (đúng!)
/// 
/// Mapping exceptions → HTTP status codes:
/// - NotFoundException     → 404 Not Found
/// - BadRequestException   → 400 Bad Request
/// - UnauthorizedException → 401 Unauthorized
/// - ValidationException   → 422 Unprocessable Entity
/// - Mọi exception khác    → 500 Internal Server Error
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// InvokeAsync được gọi cho MỌI request.
    /// 
    /// _next(context) = chuyển request sang middleware tiếp theo.
    /// Nếu không có exception → request đi bình thường.
    /// Nếu có exception → catch → trả JSON error response.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);  // Chuyển sang middleware/controller tiếp theo
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Map exception type → HTTP status code
        var (statusCode, message, errors) = exception switch
        {
            // Custom exceptions từ Application layer
            NotFoundException ex =>
                (HttpStatusCode.NotFound, ex.Message, (IDictionary<string, string[]>?)null),

            BadRequestException ex =>
                (HttpStatusCode.BadRequest, ex.Message, null),

            UnauthorizedException ex =>
                (HttpStatusCode.Unauthorized, ex.Message, null),

            // ValidationException chứa dictionary errors (field → error messages)
            Application.Common.Exceptions.ValidationException ex =>
                (HttpStatusCode.UnprocessableEntity, "Validation failed", (IDictionary<string, string[]>?)ex.Errors),

            // Mọi exception KHÔNG mong đợi → 500
            // Log chi tiết nhưng KHÔNG trả chi tiết cho client (bảo mật!)
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred", (IDictionary<string, string[]>?)null)
        };

        // Log error (500 = Error level, còn lại = Warning)
        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning("Handled exception: {ExceptionType} - {Message}", exception.GetType().Name, exception.Message);

        // Tạo response JSON chuẩn
        var response = new
        {
            status = (int)statusCode,
            message,
            errors       // null nếu không phải ValidationException
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}
