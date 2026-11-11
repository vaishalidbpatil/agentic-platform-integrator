using System.Text.Json;
using Npgsql;
using Portfolio.TradeOrderService.Configuration;
using Portfolio.TradeOrderService.Models;

namespace Portfolio.TradeOrderService.DataAccess;

public class OrderRepository : IOrderRepository
{
    private readonly DatabaseOptions _options;

    public OrderRepository(DatabaseOptions options)
    {
        _options = options;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT * FROM orders WHERE id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapOrder(reader) : null;
    }

    public async Task<Order?> GetByIdempotencyKeyAsync(string idempotencyKey)
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "SELECT * FROM orders WHERE idempotency_key = @key", conn);
        cmd.Parameters.AddWithValue("key", idempotencyKey);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapOrder(reader) : null;
    }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> ListAsync(
        string? accountId, string? symbol, OrderStatus? status, int page, int pageSize)
    {
        var conditions = new List<string>();
        var parameters = new List<NpgsqlParameter>();

        if (!string.IsNullOrEmpty(accountId))
        {
            conditions.Add("account_id = @accountId");
            parameters.Add(new NpgsqlParameter("accountId", accountId));
        }
        if (!string.IsNullOrEmpty(symbol))
        {
            conditions.Add("symbol = @symbol");
            parameters.Add(new NpgsqlParameter("symbol", symbol));
        }
        if (status.HasValue)
        {
            conditions.Add("status = @status");
            parameters.Add(new NpgsqlParameter("status", status.Value.ToString()));
        }

        var where = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
        var offset = (page - 1) * pageSize;

        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync();

        await using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM orders {where}", conn);
        countCmd.Parameters.AddRange(parameters.ToArray());
        var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

        await using var cmd = new NpgsqlCommand(
            $"SELECT * FROM orders {where} ORDER BY created_at DESC LIMIT @limit OFFSET @offset", conn);
        cmd.Parameters.AddRange(parameters.Select(p => p.Clone()).ToArray());
        cmd.Parameters.AddWithValue("limit", pageSize);
        cmd.Parameters.AddWithValue("offset", offset);

        await using var reader = await cmd.ExecuteReaderAsync();
        var orders = new List<Order>();
        while (await reader.ReadAsync())
            orders.Add(MapOrder(reader));

        return (orders, totalCount);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        // Insert order
        await using var orderCmd = new NpgsqlCommand("""
            INSERT INTO orders (id, idempotency_key, account_id, symbol, side, quantity, price, status, created_at, updated_at)
            VALUES (@id, @key, @accountId, @symbol, @side, @quantity, @price, @status, @createdAt, @updatedAt)
            RETURNING *
            """, conn, tx);

        orderCmd.Parameters.AddWithValue("id", order.Id);
        orderCmd.Parameters.AddWithValue("key", order.IdempotencyKey);
        orderCmd.Parameters.AddWithValue("accountId", order.AccountId);
        orderCmd.Parameters.AddWithValue("symbol", order.Symbol);
        orderCmd.Parameters.AddWithValue("side", order.Side.ToString());
        orderCmd.Parameters.AddWithValue("quantity", order.Quantity);
        orderCmd.Parameters.AddWithValue("price", order.Price);
        orderCmd.Parameters.AddWithValue("status", order.Status.ToString());
        orderCmd.Parameters.AddWithValue("createdAt", order.CreatedAt);
        orderCmd.Parameters.AddWithValue("updatedAt", order.UpdatedAt);

        await using var reader = await orderCmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var created = MapOrder(reader);
        await reader.CloseAsync();

        // Write outbox event in the same transaction — atomically guaranteed
        var payload = JsonSerializer.Serialize(new
        {
            created.Id,
            created.AccountId,
            created.Symbol,
            created.Side,
            created.Quantity,
            created.Price,
            created.Status,
            created.CreatedAt
        });

        await using var outboxCmd = new NpgsqlCommand("""
            INSERT INTO outbox (order_id, event_type, payload)
            VALUES (@orderId, @eventType, @payload::jsonb)
            """, conn, tx);

        outboxCmd.Parameters.AddWithValue("orderId", created.Id);
        outboxCmd.Parameters.AddWithValue("eventType", "ORDER_CREATED");
        outboxCmd.Parameters.AddWithValue("payload", payload);

        await outboxCmd.ExecuteNonQueryAsync();
        await tx.CommitAsync();

        return created;
    }

    private static Order MapOrder(NpgsqlDataReader r) => new()
    {
        Id = r.GetGuid(r.GetOrdinal("id")),
        IdempotencyKey = r.GetString(r.GetOrdinal("idempotency_key")),
        AccountId = r.GetString(r.GetOrdinal("account_id")),
        Symbol = r.GetString(r.GetOrdinal("symbol")),
        Side = Enum.Parse<Models.OrderSide>(r.GetString(r.GetOrdinal("side"))),
        Quantity = r.GetDecimal(r.GetOrdinal("quantity")),
        Price = r.GetDecimal(r.GetOrdinal("price")),
        Status = Enum.Parse<Models.OrderStatus>(r.GetString(r.GetOrdinal("status"))),
        CreatedAt = r.GetFieldValue<DateTimeOffset>(r.GetOrdinal("created_at")),
        UpdatedAt = r.GetFieldValue<DateTimeOffset>(r.GetOrdinal("updated_at"))
    };
}
