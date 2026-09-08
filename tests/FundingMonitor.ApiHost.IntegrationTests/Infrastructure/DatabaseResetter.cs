using Npgsql;
using Respawn;
using Respawn.Graph;

namespace FundingMonitor.ApiHost.IntegrationTests.Infrastructure;

public sealed class DatabaseResetter
{
    private readonly string _connectionString;
    private readonly Respawner _respawner;

    private DatabaseResetter(string connectionString, Respawner respawner)
    {
        _connectionString = connectionString;
        _respawner = respawner;
    }

    public static async Task<DatabaseResetter> CreateAsync(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        var respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["current_rates", "historical_rates"],
                TablesToIgnore =
                [
                    new Table("current_rates", "__EFMigrationsHistory"),
                    new Table("historical_rates", "__EFMigrationsHistory")
                ]
            });

        return new DatabaseResetter(connectionString, respawner);
    }

    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }
}
