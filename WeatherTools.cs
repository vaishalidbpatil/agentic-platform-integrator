using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace QuickstartWeatherServer.Tools;

[McpServerToolType]
public class WeatherTools(HttpClient client, ILogger<WeatherTools> logger)
{
    [McpServerTool, Description("Get weather alerts for a US state code.")]
    public async Task<string> GetAlerts(
        [Description("The US state code to get alerts for.")] string state)
    {
        logger.LogInformation("GetAlerts called with state={State}", state);
        using var jsonDocument = await client.ReadJsonDocumentAsync($"/alerts/active/area/{state}");
        var jsonElement = jsonDocument.RootElement;
        var alerts = jsonElement.GetProperty("features").EnumerateArray();

        if (!alerts.Any())
        {
            logger.LogInformation("GetAlerts completed: no active alerts for state={State}", state);
            return "No active alerts for this state.";
        }

        var result = string.Join("\n--\n", alerts.Select(alert =>
        {
            JsonElement properties = alert.GetProperty("properties");
            return $"""
                    Event: {properties.GetProperty("event").GetString()}
                    Area: {properties.GetProperty("areaDesc").GetString()}
                    Severity: {properties.GetProperty("severity").GetString()}
                    Description: {properties.GetProperty("description").GetString()}
                    Instruction: {properties.GetProperty("instruction").GetString()}
                    """;
        }));
        logger.LogInformation("GetAlerts completed: returned alerts for state={State}", state);
        return result;
    }

    [McpServerTool, Description("Get weather forecast for a location.")]
    public async Task<string> GetForecast(
        [Description("Latitude of the location.")] double latitude,
        [Description("Longitude of the location.")] double longitude)
    {
        logger.LogInformation("GetForecast called with latitude={Latitude}, longitude={Longitude}", latitude, longitude);
        var pointUrl = string.Create(CultureInfo.InvariantCulture, $"/points/{latitude},{longitude}");
        using var jsonDocument = await client.ReadJsonDocumentAsync(pointUrl);
        var forecastUrl = jsonDocument.RootElement.GetProperty("properties").GetProperty("forecast").GetString()
            ?? throw new InvalidOperationException($"No forecast URL provided by {client.BaseAddress}points/{latitude},{longitude}");

        using var forecastDocument = await client.ReadJsonDocumentAsync(forecastUrl);
        var periods = forecastDocument.RootElement.GetProperty("properties").GetProperty("periods").EnumerateArray();
        var result = string.Join("\n---\n", periods.Select(period => $"""
                {period.GetProperty("name").GetString()}
                Temperature: {period.GetProperty("temperature").GetInt32()}°F
                Wind: {period.GetProperty("windSpeed").GetString()} {period.GetProperty("windDirection").GetString()}
                Forecast: {period.GetProperty("detailedForecast").GetString()}
                """));
        logger.LogInformation("GetForecast completed for latitude={Latitude}, longitude={Longitude}", latitude, longitude);
        return result;
    }
}