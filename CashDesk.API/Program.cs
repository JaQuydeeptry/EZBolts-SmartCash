using Microsoft.EntityFrameworkCore;
using CashDesk.Domain;
using CashDesk.Application.Commands;
using CashDesk.Application.Interfaces;
using CashDesk.Infrastructure.Persistence;
using Scalar.AspNetCore; 

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddOpenApi(); 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("EZBolts_TestDB"));

builder.Services.AddScoped<ICashDeskRepository, CashDeskRepository>();


builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(SellProductCommand).Assembly));

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => { options.RouteTemplate = "openapi/{documentName}.json"; });
    app.UseSwaggerUI();
    
   
    app.MapScalarApiReference(options => {
        options.WithTitle("EZBolts CashDesk Management")
               .WithTheme(ScalarTheme.Moon)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}


app.UseDefaultFiles();
app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<ICashDeskRepository>();
    var testDesk = new CashDeskRoot();
    testDesk.OpenDesk(Guid.Parse("11111111-1111-1111-1111-111111111111"), 500000); 
    repo.SaveAsync(testDesk).Wait();
}

app.Run();