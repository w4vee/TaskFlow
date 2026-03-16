using MediatR;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Features.Tasks.Commands.UpdateTaskStatus;

/// <summary>
/// Command riêng chỉ để update status.
/// 
/// Tại sao tách riêng thay vì dùng UpdateTaskCommand?
/// → Trên Kanban board, user chỉ drag task từ "Todo" sang "InProgress".
///   Không cần gửi lại toàn bộ Title, Description, Priority...
///   Command nhỏ = request nhẹ = UX tốt hơn.
/// 
/// Đây là ưu điểm của CQRS: mỗi use case là 1 command riêng,
/// tối ưu cho đúng use case đó.
/// </summary>
public record UpdateTaskStatusCommand(
    Guid TaskId,
    TaskItemStatus NewStatus,
    Guid UserId
) : IRequest<TaskItemDto>;
