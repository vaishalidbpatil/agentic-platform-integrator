// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http.Headers;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Debug);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
builder.Services.AddSingleton(_ =>
{
    var client = new HttpClient() { BaseAddress = new Uri("https://api.weather.gov") };
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
    return client;
});
var app = builder.Build();
await app.RunAsync();

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(ILogger logger, string message)
    {
        logger.LogInformation("Echo called with message={Message}", message);
        var result = $"hello {message}";
        logger.LogInformation("Echo completed");
        return result;
    }

    [McpServerTool, Description("Echoes the reverse message back to the client.")]
    public static string ReverseEcho(ILogger logger, string message)
    {
        logger.LogInformation("ReverseEcho called with message={Message}", message);
        var result = $"hello {new string(message.Reverse().ToArray())}";
        logger.LogInformation("ReverseEcho completed");
        return result;
    }
}
