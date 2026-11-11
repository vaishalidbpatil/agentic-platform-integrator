using Portfolio.TradeOrderService.Configuration;
using Portfolio.TradeOrderService.DataAccess;
using Portfolio.TradeOrderService.Database;
using Portfolio.TradeOrderService.ServiceInterfaces;
using Portfolio.TradeOrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Trade Order Service", Version = "v1" });
});

var dbOptions = builder.Configuration
    .GetSection(DatabaseOptions.SectionName)
    .Get<DatabaseOptions>()
    ?? throw new InvalidOperationException("Database configuration is missing.");

builder.Services.AddSingleton(dbOptions);
builder.Services.AddSingleton<MigrationRunner>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<MigrationRunner>();
    await runner.RunAsync();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Trade Order Service v1"));

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

app.Run();
