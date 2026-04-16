using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
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

    // 1. LUỒNG GHI: Bán hàng 
    [HttpPost("{cashDeskId}/sell")]
    public async Task<IActionResult> SellProduct(Guid cashDeskId, [FromBody] SellRequest request)
    {
        var product = GetProductByCodeHandler.MockProducts.FirstOrDefault(p => p.Code == request.ProductId);
        
        if (product == null) 
            return NotFound(new { Message = "Sản phẩm không tồn tại trong hệ thống!" });

        if (product.Stock < request.Quantity)
            return BadRequest(new { Message = $"Không đủ hàng! Trong kho chỉ còn {product.Stock} sản phẩm." });

        var command = new SellProductCommand(cashDeskId, request.ProductId, request.Quantity, request.TotalPrice);
        var isSuccess = await _mediator.Send(command);

        if (isSuccess)
        {
            product.Stock -= request.Quantity;
            return Ok(new { Message = "Bán hàng thành công!", RemainingStock = product.Stock });
        }

        return BadRequest("Lỗi khi bán hàng.");
    }

    // 2. LUỒNG ĐỌC: Lấy số dư hiện tại
    [HttpGet("{cashDeskId}/balance")]
    public async Task<IActionResult> GetBalance(Guid cashDeskId)
    {
        var query = new GetBalanceQuery(cashDeskId);
        var balance = await _mediator.Send(query);

        return Ok(new
        {
            CashDeskId = cashDeskId,
            CurrentBalance = balance,
            Timestamp = DateTime.UtcNow
        });
    }

    // 3. MỞ CA
    [HttpPost("{cashDeskId}/open")]
    public async Task<IActionResult> OpenCashDesk(Guid cashDeskId, [FromQuery] decimal startingBalance = 500000 )
    {
        try
        {
            var command = new OpenCashDeskCommand(cashDeskId, startingBalance);
            var isSuccess = await _mediator.Send(command);
            return isSuccess ? Ok(new { Message = "Mở ca thành công!" }) : BadRequest("Lỗi mở ca.");
        }
        catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
    }

    // 4. KẾT THÚC CA
    [HttpPost("{cashDeskId}/close")]
    public async Task<IActionResult> CloseCashDesk(Guid cashDeskId)
    {
        try
        {
            var command = new CloseCashDeskCommand(cashDeskId);
            var isSuccess = await _mediator.Send(command);
            return isSuccess ? Ok(new { Message = "Đã khóa quầy thành công!" }) : BadRequest("Lỗi kết ca.");
        }
        catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
    }

    // 5. BÁO CÁO TỔNG HỢP 
    [HttpGet("{cashDeskId}/report")]
    public async Task<IActionResult> GetReport(Guid cashDeskId)
    {
        var events = await _dbContext.EventStreams
            .Where(e => e.AggregateId == cashDeskId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync();

        decimal startingBalance = 0;
        decimal totalRevenue = 0;
        decimal totalRefunds = 0;
        decimal totalWithdrawn = 0;
        bool isOpen = false;

        foreach (var e in events)
        {
            var details = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(e.EventData);

            switch (e.EventType)
            {
                case "CashDeskOpenedEvent":
                    startingBalance = details.GetProperty("StartingBalance").GetDecimal();
                    isOpen = true;
                    break;
                case "ProductSoldEvent":
                    totalRevenue += details.GetProperty("TotalPrice").GetDecimal();
                    break;
                case "ProductRefundedEvent":
                    totalRefunds += details.GetProperty("RefundAmount").GetDecimal();
                    break;
                case "CashWithdrawnEvent":
                    totalWithdrawn += details.GetProperty("Amount").GetDecimal();
                    break;
                case "CashDeskClosedEvent":
                    isOpen = false;
                    break;
            }
        }

        return Ok(new
        {
            StartingBalance = startingBalance,
            TotalRevenue = totalRevenue - totalRefunds,
            TotalWithdrawn = totalWithdrawn,
            CurrentBalance = startingBalance + (totalRevenue - totalRefunds) - totalWithdrawn,
            IsOpen = isOpen
        });
    }

    // 6. LỊCH SỬ SỰ KIỆN
    [HttpGet("{cashDeskId}/history")]
    public async Task<IActionResult> GetHistory(Guid cashDeskId)
    {
        var events = await _dbContext.EventStreams
            .Where(e => e.AggregateId == cashDeskId)
            .OrderByDescending(e => e.Timestamp)
            .ToListAsync();
        return Ok(events);
    }

    // 7. HOÀN TIỀN 
    [HttpPost("{cashDeskId}/refund")]
    public async Task<IActionResult> RefundProduct(Guid cashDeskId, [FromBody] RefundRequest request)
    {
        try
        {
            var command = new RefundProductCommand(cashDeskId, request.ProductId, request.Amount, request.Reason);
            var isSuccess = await _mediator.Send(command);
            
            if (isSuccess)
            {
                var product = GetProductByCodeHandler.MockProducts.FirstOrDefault(p => p.Code == request.ProductId);
                if (product != null) product.Stock += 1;
                
                return Ok(new { Message = "Đã hoàn tiền và cộng lại tồn kho!" });
            }
            return BadRequest("Lỗi hoàn tiền.");
        }
        catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
    }

    // 8. RÚT TIỀN NỘP QUỸ
    [HttpPost("{cashDeskId}/withdraw")]
    public async Task<IActionResult> WithdrawCash(Guid cashDeskId, [FromBody] WithdrawRequest request)
    {
        try
        {
            var command = new WithdrawCashCommand(cashDeskId, request.Amount, request.Reason);
            var isSuccess = await _mediator.Send(command);
            return isSuccess ? Ok(new { Message = "Rút tiền thành công!" }) : BadRequest("Lỗi rút tiền.");
        }
        catch (Exception ex) { return BadRequest(new { Error = ex.Message }); }
    }

    // 9. TRA CỨU SẢN PHẨM (ĐÃ FIX - LẤY TRỰC TIẾP TỪ KHO)
    [HttpGet("products/{code}")]
    public IActionResult GetProduct(string code)
    {
        // Fix cứng: Nhặt thẳng dữ liệu từ MockProducts, không cần qua MediatR nữa
        var product = GetProductByCodeHandler.MockProducts.FirstOrDefault(p => 
            p.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            
        return product == null ? NotFound() : Ok(product);
    }
}

// ================= DTOs & MOCK DATA =================

public record RefundRequest(string ProductId, decimal Amount, string Reason);
public record SellRequest(string ProductId, int Quantity, decimal TotalPrice);
public record WithdrawRequest(decimal Amount, string Reason);

public class ProductDto {
    public required string Code { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; } 
}

public record GetProductByCodeQuery(string Code) : IRequest<ProductDto?>;

public class GetProductByCodeHandler : IRequestHandler<GetProductByCodeQuery, ProductDto?>
{
    // ĐÃ FIX: Thêm luôn mã SP001 và SP002 vào kho cho chắc ăn
    public static readonly List<ProductDto> MockProducts = new()
    {
        new ProductDto { Code = "SP01", Name = "Cà Phê Đen Đá", Price = 20000, Stock = 50 },
        new ProductDto { Code = "SP02", Name = "Trà Sữa Trân Châu", Price = 35000, Stock = 20 },
        new ProductDto { Code = "SP03", Name = "Bánh Mì Thịt", Price = 15000, Stock = 10 },
        
        // Cụm mã 2 số 0 đề phòng gõ nhầm
        new ProductDto { Code = "SP001", Name = "Cà Phê Đen (Size L)", Price = 25000, Stock = 50 },
        new ProductDto { Code = "SP002", Name = "Trà Sữa Trân Châu (Size L)", Price = 45000, Stock = 20 }
    };

    public Task<ProductDto?> Handle(GetProductByCodeQuery request, CancellationToken cancellationToken)
    {
        var product = MockProducts.FirstOrDefault(p => 
            p.Code.Equals(request.Code, StringComparison.OrdinalIgnoreCase));
            
        return Task.FromResult(product);
    }
}