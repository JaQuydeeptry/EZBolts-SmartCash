using System;

namespace CashDesk.Domain.Events;

// 1. Sự kiện mở quầy (Nạp tiền đầu ca)
public record CashDeskOpenedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid CashDeskId { get; init; }
    public decimal StartingBalance { get; init; }
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

// 2. Sự kiện bán hàng (UR1)
public record ProductSoldEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid CashDeskId { get; init; }
    public required string ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal TotalPrice { get; init; }
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}

// 3. Sự kiện hoàn tiền (UR2)
public record ProductRefundedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid CashDeskId { get; init; }
    public required string ProductId { get; init; }
    public decimal RefundAmount { get; init; }
    public required string Reason { get; init; }
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}