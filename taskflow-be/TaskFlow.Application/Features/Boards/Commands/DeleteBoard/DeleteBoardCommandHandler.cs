using MediatR;
using TaskFlow.Application.Common.Exceptions;
using TaskFlow.Domain.Interfaces;

namespace TaskFlow.Application.Features.Boards.Commands.DeleteBoard;

public class DeleteBoardCommandHandler : IRequestHandler<DeleteBoardCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBoardCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteBoardCommand request, CancellationToken cancellationToken)
    {
        var board = await _unitOfWork.TaskBoards.GetByIdAsync(request.BoardId);
        if (board is null)
        {
            throw new NotFoundException(nameof(board), request.BoardId);
        }

        if (board.OwnerId != request.UserId)
        {
            throw new BadRequestException("You are not the owner of this board.");
        }

        await _unitOfWork.TaskBoards.DeleteAsync(board);
        await _unitOfWork.SaveChangesAsync();

        // Unit.Value = "xong rồi, không trả gì cả"
        return Unit.Value;
    }
}
