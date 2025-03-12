using PgTester.Models.QueryData;

namespace PgTester.Abstractions.Logic.Queries;

public interface IQueryFactory : IDisposable
{
    public Task<IQuery> Create(QueryData queryData, CancellationToken token);
}
