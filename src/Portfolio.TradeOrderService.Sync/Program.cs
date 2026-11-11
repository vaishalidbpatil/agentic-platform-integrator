using Portfolio.TradeOrderService.Configuration;
using Portfolio.TradeOrderService.Sync;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        var dbOptions = ctx.Configuration
            .GetSection(DatabaseOptions.SectionName)
            .Get<DatabaseOptions>()
            ?? throw new InvalidOperationException("Database configuration is missing.");

        var rabbitOptions = ctx.Configuration
            .GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>()
            ?? throw new InvalidOperationException("RabbitMq configuration is missing.");

        services.AddSingleton(dbOptions);
        services.AddSingleton(rabbitOptions);
        services.AddHostedService<OutboxRelay>();
    })
    .Build();

await host.RunAsync();
