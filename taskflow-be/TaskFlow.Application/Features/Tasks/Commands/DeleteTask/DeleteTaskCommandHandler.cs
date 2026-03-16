using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Tasks.Commands.DeleteTask;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTaskCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _unitOfWork.TaskItems.GetByIdAsync(request.TaskId);
        if (task is null)
        {
            throw new NotFoundException("TaskItem", request.TaskId);
        }

        var board = await _unitOfWork.TaskBoards.GetByIdAsync(task.BoardId);
        if (board is null || board.OwnerId != request.UserId)
        {
            throw new BadRequestException("You are not the owner of this board.");
        }

        await _unitOfWork.TaskItems.DeleteAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}
