using MediatR;
using CashDesk.Application.Interfaces;

namespace CashDesk.Application.Queries;

// 1. Cái lệnh hỏi: "Quầy này còn bao nhiêu tiền?"
public record GetBalanceQuery(Guid CashDeskId) : IRequest<decimal>;

// 2. Người trả lời lệnh hỏi
public class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, decimal>
{
    private readonly ICashDeskRepository _repository;

    public GetBalanceQueryHandler(ICashDeskRepository repository)
    {
        _repository = repository;
    }

    public async Task<decimal> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
    {
        // Phép thuật Event Sourcing: Load history -> Replay -> Lấy số dư hiện tại
        var desk = await _repository.GetByIdAsync(request.CashDeskId);
        return desk?.CurrentBalance ?? 0;
    }
}