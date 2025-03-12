using Npgsql;
using PgTester.Abstractions.Logic.Queries;
using PgTester.Models.QueryData;

namespace PgTester.Logic.Queries;

public class QueryFactory : IQueryFactory
{
    public QueryFactory(NpgsqlConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<IQuery> Create(QueryData queryData, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var queryRaw = await queryData.GetRawQuery(token);

        if (queryData is BulkQueryData bulkData)
        {
            var dataRaw = await bulkData.GetRawData(token);

            return new BulkQuery(_connection, queryRaw, dataRaw);
        }

        return new Query(_connection, queryRaw);
    }

    public void Dispose() => _connection.Dispose();
    private NpgsqlConnection _connection;
}
