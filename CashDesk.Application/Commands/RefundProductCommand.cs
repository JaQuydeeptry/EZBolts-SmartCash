using MediatR;
using CashDesk.Domain;
using CashDesk.Application.Interfaces;

namespace CashDesk.Application.Commands;

// ---------------- COMMAND ----------------
public record RefundProductCommand(
    Guid CashDeskId,
    string ProductId,
    decimal Amount,
    string Reason
) : IRequest<bool>;   

// ---------------- HANDLER ----------------
public class RefundProductCommandHandler
    : IRequestHandler<RefundProductCommand, bool>
{
    private readonly ICashDeskRepository _repository;

    public RefundProductCommandHandler(ICashDeskRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(
        RefundProductCommand request,
        CancellationToken cancellationToken)
    {
        var desk = await _repository.GetByIdAsync(request.CashDeskId);

        if (desk == null)
            throw new Exception("Không tìm thấy quầy");

        desk.RefundProduct(
            request.ProductId,
            request.Amount,
            request.Reason);

        await _repository.SaveAsync(desk);

        return true;  
    }
}