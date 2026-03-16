namespace TaskFlow.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern - ensures all repository operations 
/// are committed in a single transaction
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    ITaskBoardRepository TaskBoards { get; }
    ITaskItemRepository TaskItems { get; }
    Task<int> SaveChangesAsync();
}
