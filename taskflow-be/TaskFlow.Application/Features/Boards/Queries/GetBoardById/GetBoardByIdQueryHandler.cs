using AutoMapper;
using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Boards.Queries.GetBoardById;

public class GetBoardByIdQueryHandler : IRequestHandler<GetBoardByIdQuery, TaskBoardDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBoardByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TaskBoardDto> Handle(GetBoardByIdQuery request, CancellationToken cancellationToken)
    {
        // GetBoardWithTasksAsync: lấy board KÈM tasks (Include/eager loading)
        // Cần tasks để AutoMapper map TaskCount = Tasks.Count
        var board = await _unitOfWork.TaskBoards.GetBoardWithTasksAsync(request.BoardId);
        if (board is null)
        {
            throw new NotFoundException("TaskBoard", request.BoardId);
        }

        // Check quyền: chỉ owner mới xem được board của mình
        if (board.OwnerId != request.UserId)
        {
            throw new BadRequestException("You are not the owner of this board.");
        }

        return _mapper.Map<TaskBoardDto>(board);
    }
}
