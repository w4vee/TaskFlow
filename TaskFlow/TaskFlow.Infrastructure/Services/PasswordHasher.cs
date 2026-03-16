using TaskFlow.Application.Interfaces;

namespace TaskFlow.Infrastructure.Services;

/// <summary>
/// PasswordHasher - implement IPasswordHasher bằng BCrypt.
/// 
/// Dependency Inversion trong Clean Architecture:
/// - Application layer define IPasswordHasher (interface - "tôi CẦN gì")
/// - Infrastructure layer implement PasswordHasher (concrete - "tôi LÀM như thế nào")
/// - Nếu mai mốt muốn đổi sang Argon2 → chỉ sửa file này, Application layer không đổi
/// 
/// BCrypt work factor:
/// - WorkFactor 11 (default) ≈ ~100ms per hash → đủ chậm để chống brute force
/// - WorkFactor càng cao → càng chậm → càng an toàn nhưng user đợi lâu hơn
/// - Mỗi lần tăng 1 → thời gian gấp đôi (12 ≈ 200ms, 13 ≈ 400ms)
/// 
/// Salt:
/// - BCrypt tự generate random salt mỗi lần hash
/// - Salt được embed trong hash string → không cần lưu riêng
/// - Hash format: $2a$11$[22 chars salt][31 chars hash]
/// - Cùng password "123456" → 2 lần hash ra 2 string KHÁC NHAU (do salt khác)
/// - Verify vẫn match vì BCrypt extract salt từ hash rồi hash lại để so sánh
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 11;

    /// <summary>
    /// Hash password bằng BCrypt.
    /// Input:  "123456"
    /// Output: "$2a$11$rK7T5xGH3kJ9..." (60 chars, chứa cả salt)
    /// </summary>
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verify password vs hash.
    /// BCrypt extract salt từ hash → hash password với salt đó → so sánh.
    /// Return true nếu match, false nếu không.
    /// </summary>
    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
