using PgTester.Models.QueryData;

namespace PgTester.Abstractions.Logic.Queries;

public interface IQueryFactory : IDisposable
{
    public Task<IQuery> CreateAsync(QueryData queryData, CancellationToken token);
}
