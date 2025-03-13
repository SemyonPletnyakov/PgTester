using PgTester.Abstractions.Logic.Queries;
using PgTester.Models;

namespace PgTester.Abstractions.Logic;

public interface IExperiment
{
    public Task<Statistic> ExecuteAsync(
        IQueryFactory queryFactory, 
        CancellationToken cancellation);
}
