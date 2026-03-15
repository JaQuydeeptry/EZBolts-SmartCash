using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using CashDesk.Application.Interfaces;

namespace CashDesk.Application.Commands;


public record SellProductCommand(
    Guid CashDeskId, 
    string ProductId, 
    int Quantity, 
    decimal TotalPrice
) : IRequest<bool>;


public class SellProductCommandHandler : IRequestHandler<SellProductCommand, bool>
{
    private readonly ICashDeskRepository _repository;

    
    public SellProductCommandHandler(ICashDeskRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(SellProductCommand request, CancellationToken cancellationToken)
    {
        
        var desk = await _repository.GetByIdAsync(request.CashDeskId);
        
        if (desk == null)
            throw new Exception("Không tìm thấy quầy thu ngân này!");

        
        desk.SellProduct(request.ProductId, request.Quantity, request.TotalPrice);

        
        await _repository.SaveAsync(desk);

        return true; 
    }
}