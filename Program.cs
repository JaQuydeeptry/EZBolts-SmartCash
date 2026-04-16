using Microsoft.EntityFrameworkCore;
using CashDesk.Domain;
using CashDesk.Application.Commands;
using CashDesk.Application.Interfaces;
using CashDesk.Infrastructure.Persistence;
using Scalar.AspNetCore; 
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// --- CẤU HÌNH SERVICES ---
builder.Services.AddControllers();
builder.Services.AddOpenApi(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("EZBolts_TestDB"));

builder.Services.AddScoped<IEventStore, EventStore>();
builder.Services.AddScoped<ICashDeskRepository, CashDeskRepository>();

builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(SellProductCommand).Assembly));

var app = builder.Build();

// --- CẤU HÌNH MIDDLEWARE (PIPELINE) ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    app.MapScalarApiReference(options => {
        options.WithTitle("EZBolts CashDesk Management")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// 💥 ĐIỂM QUAN TRỌNG: Tự động tìm file index.html trong thư mục wwwroot
app.UseDefaultFiles(); 
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// --- SEED DỮ LIỆU BAN ĐẦU ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try 
    {
        var eventStore = services.GetRequiredService<IEventStore>();
        var deskId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        
        var history = await eventStore.GetEventsAsync(deskId);
        if (!history.Any())
        {
            var testDesk = new CashDeskRoot();
            testDesk.OpenDesk(deskId, 500000); 
            await eventStore.SaveEventsAsync(deskId, testDesk.GetUncommittedEvents());
            Console.WriteLine(">>> [Hệ thống] Đã tự động mở ca với vốn 500.000 VNĐ");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> [Lỗi Seed dữ liệu]: {ex.Message}");
    }
}

app.Run();