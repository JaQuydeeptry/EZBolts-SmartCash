using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CashDesk.Application.Commands;
using CashDesk.Application.Queries;
using CashDesk.Infrastructure.Persistence; 
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CashDesk.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CashDeskController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ApplicationDbContext _dbContext; 

    public CashDeskController(IMediator mediator, ApplicationDbContext dbContext)
    {
        _mediator = mediator;
        _dbContext = dbContext;
    }

    // 1. LUỒNG GHI (COMMAND): Khi thu ngân bấm "Thanh toán"
    [HttpPost("{cashDeskId}/sell")]
    public async Task<IActionResult> SellProduct(Guid cashDeskId, [FromBody] SellRequest request)
    {
        var command = new SellProductCommand(cashDeskId, request.ProductId, request.Quantity, request.TotalPrice);
        
        var isSuccess = await _mediator.Send(command);

        if (isSuccess)
            return Ok(new { Message = "Bán hàng thành công! Đã lưu Event Sourcing." });
        
        return BadRequest("Lỗi khi bán hàng.");
    }

    // 2. LUỒNG ĐỌC (QUERY): Xem số dư 
    [HttpGet("{cashDeskId}/balance")]
    public async Task<IActionResult> GetBalance(Guid cashDeskId)
    {
        var query = new GetBalanceQuery(cashDeskId);
        var balance = await _mediator.Send(query);
        
        return Ok(new 
        { 
            CashDeskId = cashDeskId, 
            CurrentBalance = balance,
            Unit = "VND",
            Timestamp = DateTime.UtcNow 
        });
    }

    // 3. XEM LỊCH SỬ EVENT (Event Sourcing)
    [HttpGet("{cashDeskId}/history")]
    public async Task<IActionResult> GetHistory(Guid cashDeskId)
    {
       
        var events = await _dbContext.EventStreams
            .Where(e => e.AggregateId == cashDeskId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        return Ok(events);
    }
}

public record SellRequest(string ProductId, int Quantity, decimal TotalPrice);