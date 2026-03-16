using AutoMapper;
using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Tasks.Commands.UpdateTask;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskItemDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateTaskCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TaskItemDto> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm task
        var task = await _unitOfWork.TaskItems.GetByIdAsync(request.TaskId);
        if (task is null)
        {
            throw new NotFoundException("TaskItem", request.TaskId);
        }

        // 2. Verify user là owner của board chứa task này
        var board = await _unitOfWork.TaskBoards.GetByIdAsync(task.BoardId);
        if (board is null || board.OwnerId != request.UserId)
        {
            throw new BadRequestException("You are not the owner of this board.");
        }

        // 3. Nếu có AssignedToId, verify user đó tồn tại
        if (request.AssignedToId.HasValue)
        {
            var exists = await _unitOfWork.Users.ExistsAsync(request.AssignedToId.Value);
            if (!exists)
            {
                throw new NotFoundException("User", request.AssignedToId.Value);
            }
        }

        // 4. Update properties
        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.Deadline = request.Deadline;
        task.AssignedToId = request.AssignedToId;

        await _unitOfWork.TaskItems.UpdateAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<TaskItemDto>(task);
    }
}
