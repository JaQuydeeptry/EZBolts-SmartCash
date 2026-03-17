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

    // 3. MỞ CA
    [HttpPost("{cashDeskId}/open")]
    public async Task<IActionResult> OpenCashDesk(Guid cashDeskId, [FromQuery] decimal startingBalance = 100000)
    {
        try
        {
            var command = new OpenCashDeskCommand(cashDeskId, startingBalance);
            var isSuccess = await _mediator.Send(command);

            if (isSuccess)
                return Ok(new { Message = $"Đã mở ca thành công với số vốn {startingBalance} VNĐ" });

            return BadRequest("Lỗi khi mở ca.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Lỗi hệ thống khi mở ca", Details = ex.Message });
        }
    }

    // 4. KẾT THÚC CA
    [HttpPost("{cashDeskId}/close")]
    public async Task<IActionResult> CloseCashDesk(Guid cashDeskId)
    {
        try
        {
            var command = new CloseCashDeskCommand(cashDeskId);
            var isSuccess = await _mediator.Send(command);

            if (isSuccess)
                return Ok(new { Message = "Đã kết thúc ca thành công! Quầy thu ngân đã được khóa." });

            return BadRequest("Lỗi khi kết thúc ca.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Lỗi hệ thống khi kết thúc ca", Details = ex.Message });
        }
    }

    // 5. BÁO CÁO: Tiền vốn vs Doanh thu
    [HttpGet("{cashDeskId}/report")]
    public async Task<IActionResult> GetReport(Guid cashDeskId)
    {
        var events = await _dbContext.EventStreams
            .Where(e => e.AggregateId == cashDeskId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        decimal startingBalance = 0;
        decimal totalRevenue = 0;
        bool isOpen = false;

        foreach (var e in events)
        {
            var details = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(e.EventData);
            
            if (e.EventType == "CashDeskOpenedEvent")
            {
                startingBalance = details.GetProperty("StartingBalance").GetDecimal();
                isOpen = true;
            }
            else if (e.EventType == "ProductSoldEvent")
            {
                totalRevenue += details.GetProperty("TotalPrice").GetDecimal();
            }
            else if (e.EventType == "CashDeskClosedEvent")
            {
                isOpen = false;
            }
        }

        return Ok(new 
        { 
            CashDeskId = cashDeskId,
            StartingBalance = startingBalance,
            TotalRevenue = totalRevenue,
            CurrentBalance = startingBalance + totalRevenue,
            IsOpen = isOpen,
            Timestamp = DateTime.UtcNow
        });
    }

    // 5. XEM LỊCH SỬ EVENT (Event Sourcing)
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