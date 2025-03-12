using Npgsql;
using PgTester.Abstractions.Logic.Queries;

namespace PgTester.Logic.Queries;

public sealed class BulkQuery : IQuery
{
    public BulkQuery(NpgsqlConnection connection, string query, string queryData)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _query = query ?? throw new ArgumentNullException(nameof(query));
        _queryData = queryData ?? throw new ArgumentNullException(nameof(queryData));
    }

    public async Task ExecuteAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        using var writer = await _connection.BeginTextImportAsync(_query, token);
        await writer.WriteAsync(_queryData);
    }

    public void Dispose() => _connection.Dispose();

    private NpgsqlConnection _connection;
    private string _query;
    private string _queryData;
}
