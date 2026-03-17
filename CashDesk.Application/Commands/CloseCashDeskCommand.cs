using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using CashDesk.Application.Interfaces;

namespace CashDesk.Application.Commands;

public record CloseCashDeskCommand(Guid CashDeskId) : IRequest<bool>;

public class CloseCashDeskCommandHandler : IRequestHandler<CloseCashDeskCommand, bool>
{
    private readonly ICashDeskRepository _repository;

    public CloseCashDeskCommandHandler(ICashDeskRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(CloseCashDeskCommand request, CancellationToken cancellationToken)
    {
        var desk = await _repository.GetByIdAsync(request.CashDeskId);

        if (desk == null)
            throw new Exception("Không tìm thấy quầy thu ngân này!");

        desk.CloseDesk();

        await _repository.SaveAsync(desk);

        return true;
    }
}