using System.Reflection;
using Npgsql;
using Portfolio.TradeOrderService.Configuration;

namespace Portfolio.TradeOrderService.Database;

public class MigrationRunner
{
    private readonly DatabaseOptions _options;

    public MigrationRunner(DatabaseOptions options)
    {
        _options = options;
    }

    public async Task RunAsync()
    {
        await EnsureMigrationsTableAsync();

        var applied = await GetAppliedMigrationsAsync();
        var migrations = GetMigrationFiles();

        foreach (var (name, sql) in migrations)
        {
            if (applied.Contains(name))
                continue;

            await ApplyMigrationAsync(name, sql);
            Console.WriteLine($"Applied migration: {name}");
        }
    }

    private IEnumerable<(string Name, string Sql)> GetMigrationFiles()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceNames()
            .Where(r => r.Contains(".Migrations.") && r.EndsWith(".sql"))
            .OrderBy(r => r)
            .Select(r =>
            {
                using var stream = assembly.GetManifestResourceStream(r)!;
                using var reader = new StreamReader(stream);
                var name = r.Split(".Migrations.").Last();
                return (name, reader.ReadToEnd());
            });
    }

    private async Task EnsureMigrationsTableAsync()
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS __migrations (
                name       VARCHAR(256) PRIMARY KEY,
                applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );
            """;
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<HashSet<string>> GetAppliedMigrationsAsync()
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("SELECT name FROM __migrations", conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        var applied = new HashSet<string>();
        while (await reader.ReadAsync())
            applied.Add(reader.GetString(0));
        return applied;
    }

    private async Task ApplyMigrationAsync(string name, string sql)
    {
        await using var conn = new NpgsqlConnection(_options.ConnectionString);
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync();
        await using var record = new NpgsqlCommand(
            "INSERT INTO __migrations (name) VALUES (@name)", conn, tx);
        record.Parameters.AddWithValue("name", name);
        await record.ExecuteNonQueryAsync();
        await tx.CommitAsync();
    }
}
