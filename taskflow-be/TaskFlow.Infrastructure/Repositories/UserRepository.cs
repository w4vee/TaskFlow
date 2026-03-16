using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Interfaces;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

/// <summary>
/// UserRepository - kế thừa GenericRepository (có sẵn CRUD) + thêm methods riêng cho User.
/// 
/// Pattern: GenericRepository<User> cung cấp GetById, GetAll, Add, Update, Delete.
///          IUserRepository thêm GetByEmail, GetByRefreshToken, EmailExists.
/// 
/// So sánh với SWallet:
/// - SWallet: UserService chứa cả business logic + data access
/// - TaskFlow: UserRepository CHỈ chứa data access, business logic ở Handler
/// </summary>
public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Tìm user theo email (dùng khi Login).
    /// SingleOrDefaultAsync: trả về 1 entity hoặc null.
    /// - Dùng Single thay vì First vì email là unique → chỉ có tối đa 1 kết quả.
    /// - Nếu có 2+ kết quả → throw exception (phát hiện bug data).
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.SingleOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Tìm user theo refresh token (dùng khi Refresh Token).
    /// </summary>
    public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
    {
        return await _dbSet.SingleOrDefaultAsync(u => u.RefreshToken == refreshToken);
    }

    /// <summary>
    /// Kiểm tra email đã tồn tại chưa (dùng khi Register).
    /// AnyAsync hiệu quả hơn GetByEmailAsync vì không load entity,
    /// chỉ return true/false.
    /// </summary>
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email);
    }
}
