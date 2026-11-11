using System.Text;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Portfolio.TradeOrderService.Configuration;
using RabbitMQ.Client;

namespace Portfolio.TradeOrderService.Sync;

public class OutboxRelay : BackgroundService
{
    private readonly DatabaseOptions _dbOptions;
    private readonly RabbitMqOptions _mqOptions;
    private readonly ILogger<OutboxRelay> _logger;
    private readonly ResiliencePipeline _publishPipeline;

    public OutboxRelay(DatabaseOptions dbOptions, RabbitMqOptions mqOptions, ILogger<OutboxRelay> logger)
    {
        _dbOptions = dbOptions;
        _mqOptions = mqOptions;
        _logger = logger;

        // Retry 3x with exponential backoff, then open circuit after sustained failures
        _publishPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                OnRetry = args =>
                {
                    _logger.LogWarning("RabbitMQ publish retry {Attempt} after {Delay}",
                        args.AttemptNumber, args.RetryDelay);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 3,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpened = args =>
                {
                    _logger.LogError("RabbitMQ circuit breaker opened — pausing publishes for {Duration}", args.BreakDuration);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxRelay starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunRelayLoopAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Relay loop failed — reconnecting in 10s");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("OutboxRelay stopped");
    }

    private async Task RunRelayLoopAsync(CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            HostName = _mqOptions.Host,
            Port = _mqOptions.Port,
            UserName = _mqOptions.Username,
            Password = _mqOptions.Password
        };

        await using var connection = await factory.CreateConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(_mqOptions.Exchange, ExchangeType.Direct, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(_mqOptions.Queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(_mqOptions.Queue, _mqOptions.Exchange, "order.created", cancellationToken: ct);

        _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}, polling outbox", _mqOptions.Host, _mqOptions.Port);

        while (!ct.IsCancellationRequested)
        {
            await ProcessBatchAsync(channel, ct);
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }

    private async Task ProcessBatchAsync(IChannel channel, CancellationToken ct)
    {
        var entries = await FetchPendingAsync(ct);
        if (entries.Count == 0) return;

        _logger.LogInformation("Processing {Count} pending outbox entries", entries.Count);

        foreach (var entry in entries)
        {
            try
            {
                await _publishPipeline.ExecuteAsync(async token =>
                {
                    var body = Encoding.UTF8.GetBytes(entry.Payload);
                    var props = new BasicProperties { Persistent = true };
                    await channel.BasicPublishAsync(_mqOptions.Exchange, "order.created", true, props, body, token);
                }, ct);

                await MarkSentAsync(entry.Id, ct);
                _logger.LogInformation("Published {EventType} for order {OrderId}", entry.EventType, entry.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox entry {Id} after retries", entry.Id);
                await MarkFailedAsync(entry.Id, ct);
            }
        }
    }

    private async Task<List<OutboxEntry>> FetchPendingAsync(CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_dbOptions.ConnectionString);
        await conn.OpenAsync(ct);

        // FOR UPDATE SKIP LOCKED allows multiple relay instances to run safely without duplicates
        await using var cmd = new NpgsqlCommand("""
            SELECT id, order_id, event_type, payload, attempts
            FROM outbox
            WHERE status = 'PENDING' AND attempts < 5
            ORDER BY created_at ASC
            LIMIT 20
            FOR UPDATE SKIP LOCKED
            """, conn);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        var entries = new List<OutboxEntry>();
        while (await reader.ReadAsync(ct))
        {
            entries.Add(new OutboxEntry(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4)
            ));
        }
        return entries;
    }

    private async Task MarkSentAsync(Guid id, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_dbOptions.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "UPDATE outbox SET status = 'SENT', sent_at = now() WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private async Task MarkFailedAsync(Guid id, CancellationToken ct)
    {
        await using var conn = new NpgsqlConnection(_dbOptions.ConnectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand(
            "UPDATE outbox SET attempts = attempts + 1, status = CASE WHEN attempts + 1 >= 5 THEN 'FAILED' ELSE 'PENDING' END WHERE id = @id",
            conn);
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private record OutboxEntry(Guid Id, Guid OrderId, string EventType, string Payload, int Attempts);
}
