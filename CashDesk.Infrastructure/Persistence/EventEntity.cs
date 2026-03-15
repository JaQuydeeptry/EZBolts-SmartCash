using System;

namespace CashDesk.Infrastructure.Persistence;


public class EventEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AggregateId { get; set; } 
    public string EventType { get; set; } = string.Empty; 
    public string EventData { get; set; } = string.Empty; 
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}