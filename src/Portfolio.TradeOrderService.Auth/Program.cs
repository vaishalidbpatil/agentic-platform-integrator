var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddIdentityServer(options =>
    {
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseSuccessEvents = true;
        options.Events.RaiseInformationEvents = true;
    })
    .AddInMemoryIdentityResources(Portfolio.TradeOrderService.Auth.Config.IdentityResources)
    .AddInMemoryApiScopes(Portfolio.TradeOrderService.Auth.Config.ApiScopes)
    .AddInMemoryApiResources(Portfolio.TradeOrderService.Auth.Config.ApiResources)
    .AddInMemoryClients(Portfolio.TradeOrderService.Auth.Config.Clients)
    .AddDeveloperSigningCredential();  // dev only — swap for X.509 cert in production

var app = builder.Build();

app.UseIdentityServer();

// Discovery doc and JWKS are served automatically at /.well-known/openid-configuration
app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

app.Run();
