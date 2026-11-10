// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;

Console.OutputEncoding = System.Text.Encoding.UTF8;

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

if (Environment.GetEnvironmentVariable("DEBUG_MCP_SERVER") == "1")
{
    Console.Error.WriteLine($"[MCP DEBUG] Waiting for debugger — PID={Environment.ProcessId}");
    while (!Debugger.IsAttached)
        Thread.Sleep(200);
    Debugger.Break();
}

await app.RunAsync();

[McpServerToolType]
public class EchoTool(ILogger<EchoTool> logger)
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public string Echo(string message)
    {
        logger.LogInformation("Echo called with message={Message}", message);
        var result = $"hello from Vaishali :  {message}";
        logger.LogInformation("Echo completed");
        return result;
    }

    [McpServerTool, Description("Echoes the reverse message back to the client.")]
    public string ReverseEcho(string message)
    {
        logger.LogInformation("ReverseEcho called with message={Message}", message);
        var result = $"hello from Vaishali {new string(message.Reverse().ToArray())}";
        logger.LogInformation("ReverseEcho completed");
        return result;
    }
}
