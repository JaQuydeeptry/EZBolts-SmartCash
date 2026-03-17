using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using CashDesk.Application.Interfaces;
using CashDesk.Domain;

namespace CashDesk.Application.Commands;

public record OpenCashDeskCommand(Guid CashDeskId, decimal StartingBalance) : IRequest<bool>;

public class OpenCashDeskCommandHandler : IRequestHandler<OpenCashDeskCommand, bool>
{
    private readonly ICashDeskRepository _repository;

    public OpenCashDeskCommandHandler(ICashDeskRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(OpenCashDeskCommand request, CancellationToken cancellationToken)
    {
        var desk = await _repository.GetByIdAsync(request.CashDeskId);

        if (desk == null)
        {
            desk = new CashDeskRoot();
        }

        desk.OpenDesk(request.CashDeskId, request.StartingBalance);

        await _repository.SaveAsync(desk);

        return true;
    }
}