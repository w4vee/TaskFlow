using AutoMapper;
using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Tasks.Commands.UpdateTaskStatus;

public class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand, TaskItemDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateTaskStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TaskItemDto> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
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

        // Chỉ update status, không đụng gì khác
        task.Status = request.NewStatus;

        await _unitOfWork.TaskItems.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<TaskItemDto>(task);
    }
}
