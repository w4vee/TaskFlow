using AutoMapper;
using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Application.DTOs;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Boards.Commands.UpdateBoard;

public class UpdateBoardCommandHandler : IRequestHandler<UpdateBoardCommand, TaskBoardDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateBoardCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<TaskBoardDto> Handle(UpdateBoardCommand request, CancellationToken cancellationToken)
    {
        // 1. Tìm board
        var board = await _unitOfWork.TaskBoards.GetByIdAsync(request.BoardId);
        if (board is null)
        {
            throw new NotFoundException(nameof(board), request.BoardId);
        }

        // 2. Check quyền: chỉ owner mới được update
        if (board.OwnerId != request.UserId)
        {
            throw new BadRequestException("You are not the owner of this board.");
        }

        // 3. Update properties
        board.Name = request.Name;
        board.Description = request.Description;

        await _unitOfWork.TaskBoards.UpdateAsync(board);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<TaskBoardDto>(board);
    }
}
