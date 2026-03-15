using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CashDesk.Application.Interfaces;
using CashDesk.Domain;
using CashDesk.Domain.Events;

namespace CashDesk.Infrastructure.Persistence;

public class CashDeskRepository : ICashDeskRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CashDeskRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    
    public async Task<CashDeskRoot> GetByIdAsync(Guid cashDeskId)
    {
        
        var eventEntities = await _dbContext.EventStreams
            .Where(e => e.AggregateId == cashDeskId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        var history = new List<IEvent>();

        
        foreach (var entity in eventEntities)
        {
            var type = Type.GetType($"CashDesk.Domain.Events.{entity.EventType}, CashDesk.Domain");
            if (type != null)
            {
                var @event = JsonSerializer.Deserialize(entity.EventData, type) as IEvent;
                if (@event != null) history.Add(@event);
            }
        }

        
        var cashDesk = new CashDeskRoot();
        cashDesk.LoadFromHistory(history);

        return cashDesk;
    }

    
    public async Task SaveAsync(CashDeskRoot cashDesk)
    {
        
        var newEvents = cashDesk.GetUncommittedEvents();

        foreach (var @event in newEvents)
        {
            var eventEntity = new EventEntity
            {
                AggregateId = cashDesk.Id,
                EventType = @event.GetType().Name,
               
                EventData = JsonSerializer.Serialize((object)@event) 
            };
            _dbContext.EventStreams.Add(eventEntity);
        }

        await _dbContext.SaveChangesAsync();
        
       
        cashDesk.ClearUncommittedEvents();
    }
}