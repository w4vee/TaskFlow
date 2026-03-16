namespace TaskFlow.Application.Common.Exceptions;

/// <summary>
/// Throw khi user chưa đăng nhập hoặc token không hợp lệ.
/// Middleware sẽ map exception này → HTTP 401 Unauthorized.
/// 
/// Ví dụ: throw new UnauthorizedException("Invalid email or password");
/// </summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message)
        : base(message)
    {
    }
}
