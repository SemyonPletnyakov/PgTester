using PgTester.Abstractions.Logic;
using PgTester.Abstractions.Logic.Queries;
using PgTester.Models;
using PgTester.Models.QueryData;

namespace PgTester.Logic;

public sealed class Experiment : IExperiment
{
    public Experiment(
        IEnumerable<QueryData> preparatoryQueriesData,
        IEnumerable<QueryData> targetQueriesData)
    {
        _preparatoryQueriesData = 
            preparatoryQueriesData ?? throw new ArgumentNullException(nameof(preparatoryQueriesData));
        _targetQueriesData = targetQueriesData ?? throw new ArgumentNullException(nameof(targetQueriesData));
    }

    public async Task<Statistic> ExecuteAsync(
        IQueryFactory queryFactory,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var queryData in _preparatoryQueriesData)
        {
            var query = await queryFactory.CreateAsync(queryData, cancellationToken);
            await query.ExecuteAsync(cancellationToken);
        }

        var queries = await Task.WhenAll(
            _targetQueriesData.Select(data => 
                queryFactory.CreateAsync(data, cancellationToken)));

        var statisticCollector  = new StatisticCollector(); // [TODO] переместить создание в фабрику, когда будет собирать статистику из постгре

        statisticCollector.Start();
        
        foreach(var query in queries)
        {
            await query.ExecuteAsync(cancellationToken);
        }

        statisticCollector.Stop();

        return statisticCollector.GetStatistic();
    }

    private readonly IEnumerable<QueryData> _preparatoryQueriesData;
    private readonly IEnumerable<QueryData> _targetQueriesData;
}
