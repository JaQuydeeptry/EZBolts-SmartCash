using System;
using System.Threading.Tasks;
using CashDesk.Domain;

namespace CashDesk.Application.Interfaces;

public interface ICashDeskRepository
{
        Task<CashDeskRoot> GetByIdAsync(Guid cashDeskId);

   
    Task SaveAsync(CashDeskRoot cashDesk);
}