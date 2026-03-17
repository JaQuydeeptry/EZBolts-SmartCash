using System;

namespace CashDesk.Domain.Events;

public interface IEvent
{

    Guid EventId { get; }
    DateTime OccurredOn { get; }
}