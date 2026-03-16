using FluentValidation;
using MediatR;
using ValidationException = TaskFlow.Application.Common.Exceptions.ValidationException;

namespace TaskFlow.Application.Common.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior - chạy TỰ ĐỘNG trước MỌI Handler.
/// 
/// Flow: Controller → Send(Command) → [ValidationBehavior] → Handler
/// 
/// Cách hoạt động:
/// 1. MediatR nhận Command
/// 2. Trước khi gọi Handler, nó chạy qua ValidationBehavior
/// 3. ValidationBehavior tìm tất cả Validator cho Command đó (qua DI)
/// 4. Nếu có lỗi → throw ValidationException (Handler KHÔNG được gọi)
/// 5. Nếu OK → gọi next() để chuyển sang Handler
/// 
/// IPipelineBehavior&lt;TRequest, TResponse&gt;:
/// - TRequest: kiểu Command/Query
/// - TResponse: kiểu kết quả trả về
/// Nó "bọc" quanh Handler, giống middleware bọc quanh endpoint.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    // DI inject TẤT CẢ validators cho TRequest
    // Ví dụ: nếu TRequest là RegisterCommand, nó sẽ inject RegisterCommandValidator
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next, // next = gọi Handler tiếp theo
        CancellationToken cancellationToken)
    {
        // Nếu không có validator nào cho request này → skip, gọi Handler luôn
        if (!_validators.Any())
        {
            return await next();
        }

        // Chạy tất cả validators
        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        // Gom tất cả lỗi từ mọi validator
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            // Group lỗi theo property name
            // Ví dụ: { "Email": ["Email is required", "Email format invalid"] }
            var errorDictionary = failures
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            throw new ValidationException(errorDictionary);
        }

        // Validation pass → gọi Handler
        return await next();
    }
}
