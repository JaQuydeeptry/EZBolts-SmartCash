using System;
using System.Collections.Generic;
using CashDesk.Domain.Events;

namespace CashDesk.Domain;

public class CashDeskRoot
{
    
    public Guid Id { get; private set; }
    public decimal CurrentBalance { get; private set; }

    
    private readonly List<IEvent> _uncommittedEvents = new();

    public IReadOnlyList<IEvent> GetUncommittedEvents() => _uncommittedEvents.AsReadOnly();
    public void ClearUncommittedEvents() => _uncommittedEvents.Clear();

        public void LoadFromHistory(IEnumerable<IEvent> history)
    {
        foreach (var e in history)
        {
            ApplyEvent(e, isNew: false);
        }
    }

   
    public void OpenDesk(Guid deskId, decimal startingBalance)
    {
        if (startingBalance < 0) throw new InvalidOperationException("Tiền đầu ca không được âm!");
        
        var @event = new CashDeskOpenedEvent
        {
            CashDeskId = deskId,
            StartingBalance = startingBalance
        };
        ApplyEvent(@event, isNew: true);
    }

    public void SellProduct(string productId, int quantity, decimal totalPrice)
    {
        if (totalPrice <= 0) throw new InvalidOperationException("Giá trị đơn hàng phải lớn hơn 0!");

        var @event = new ProductSoldEvent
        {
            CashDeskId = this.Id,
            ProductId = productId,
            Quantity = quantity,
            TotalPrice = totalPrice
        };
        ApplyEvent(@event, isNew: true);
    }

    
    private void ApplyEvent(IEvent @event, bool isNew)
    {
        
        switch (@event)
        {
            case CashDeskOpenedEvent e:
                Id = e.CashDeskId;
                CurrentBalance = e.StartingBalance;
                break;
            case ProductSoldEvent e:
                CurrentBalance += e.TotalPrice; 
                break;
            case ProductRefundedEvent e:
                CurrentBalance -= e.RefundAmount; 
                break;
        }

       
        if (isNew)
        {
            _uncommittedEvents.Add(@event);
        }
    }
}