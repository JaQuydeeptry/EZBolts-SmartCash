using System;
using System.Collections.Generic;
using CashDesk.Domain.Events;

namespace CashDesk.Domain;

public class CashDeskRoot
{
    
    public Guid Id { get; private set; }
    public decimal CurrentBalance { get; private set; }
    public bool IsOpen { get; private set; } = false;
    public decimal StartingBalance { get; private set; }
    
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
        if (IsOpen) throw new InvalidOperationException("Quầy đang mở, không thể mở lại.");
        
        var @event = new CashDeskOpenedEvent
        {
            CashDeskId = deskId,
            StartingBalance = startingBalance
        };
        ApplyEvent(@event, isNew: true);
    }

    public void SellProduct(string productId, int quantity, decimal totalPrice)
    {
        if (!IsOpen) throw new InvalidOperationException("Quầy đã đóng, không thể thực hiện giao dịch!");
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

    public void CloseDesk()
    {
        if (!IsOpen) throw new InvalidOperationException("Quầy đã đóng rồi!");

        var @event = new CashDeskClosedEvent
        {
            CashDeskId = this.Id,
            FinalBalance = this.CurrentBalance
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
                StartingBalance = e.StartingBalance;
                IsOpen = true;
                break;
            case ProductSoldEvent e:
                CurrentBalance += e.TotalPrice; 
                break;
            case ProductRefundedEvent e:
                CurrentBalance -= e.RefundAmount; 
                break;
            case CashDeskClosedEvent e:
                IsOpen = false;
                break;
        }

       
        if (isNew)
        {
            _uncommittedEvents.Add(@event);
        }
    }
}