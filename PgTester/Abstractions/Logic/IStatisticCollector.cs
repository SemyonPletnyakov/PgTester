using PgTester.Models;
using System.Diagnostics;

namespace PgTester.Abstractions.Logic;

public interface IStatisticCollector
{
    public void Start();
    public void Stop();
    public Statistic GetStatistic();
}
