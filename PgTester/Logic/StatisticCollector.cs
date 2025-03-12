using PgTester.Abstractions.Logic;
using PgTester.Models;

using System.Diagnostics;

namespace PgTester.Logic;

public sealed class StatisticCollector : IStatisticCollector
{
    public void Start() => _stopwatch.Start();

    public void Stop() => _stopwatch.Stop();

    public Statistic GetStatistic() => new(_stopwatch.Elapsed);

    private Stopwatch _stopwatch = new();
}
