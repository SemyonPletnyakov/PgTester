using Npgsql;
using PgTester.Abstractions.Logic;
using PgTester.Logic;
using PgTester.Logic.Queries;
using PgTester.Models;
using PgTester.Models.QueryData;
using System.Diagnostics.CodeAnalysis;
using System.ServiceProcess;

var defaultSettings = new Settings(
    new string[]
    {
        "shared_buffers = 3GB",
        "wal_buffers = 96MB",
        "checkpoint_completion_target = 0.9",
        "checkpoint_timeout = 5min",
        "max_wal_size = 1GB",
        "bgwriter_delay = 200ms",
        "bgwriter_lru_maxpages = 100",
        "autovacuum_naptime = 1min",
        "autovacuum_vacuum_insert_threshold = 1000",
        "autovacuum_vacuum_insert_scale_factor = 0.2",
        "autovacuum_vacuum_threshold = 50",
        "autovacuum_vacuum_scale_factor = 0.2",
        "autovacuum_analyze_threshold = 50",
        "autovacuum_analyze_scale_factor = 0.1",
    }.ToDictionary(str => str.Split(' ')[0], str => str));

var customSettingsList = new string[][]
{
    [],
    ["wal_buffers = 32MB"],
    ["wal_buffers = 500MB"],
    ["checkpoint_timeout = 30s"],
    ["max_wal_size = 100MB"],
    [
        "autovacuum_naptime = 2min",
        "autovacuum_vacuum_insert_threshold = 10000",
        "autovacuum_vacuum_insert_scale_factor = 0.3"
    ],
    [
        "autovacuum_naptime = 2min",
        "autovacuum_vacuum_insert_threshold = 50000",
        "autovacuum_vacuum_insert_scale_factor = 0.4"
    ],
    [
        "autovacuum_naptime = 2min",
        "autovacuum_vacuum_threshold = 500",
        "autovacuum_vacuum_scale_factor = 0.3",
    ],
    [
        "autovacuum_naptime = 2min",
        "autovacuum_vacuum_threshold = 5000",
        "autovacuum_vacuum_scale_factor = 0.4",
    ],
    [
        "autovacuum_analyze_threshold = 500",
        "autovacuum_analyze_scale_factor = 0.2"
    ],
    [
        "autovacuum_analyze_threshold = 5000",
        "autovacuum_analyze_scale_factor = 0.3"
    ],
}.Select(settings =>
        new Settings(settings.ToDictionary(str => str.Split(' ')[0], str => str)))
    .ToArray();

using var defaultSettingsReader = new StreamReader("..\\..\\..\\..\\Common\\postgresql.conf");
var defaultSettingsRaw = defaultSettingsReader.ReadToEnd();

var dropTableAndIndex = new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\drop_table_and_index.sql");
var createTable = new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\create_table.sql");
var createIndex = new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\create_index.sql");

var inserts = new QueryData[]
{
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\insert_common.sql"),
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\insert_package.sql"),
    new BulkQueryData(
        "..\\..\\..\\..\\Common\\SqlScripts\\insert_bulk.sql",
        "..\\..\\..\\..\\Common\\SqlScripts\\insert_bulk_data.csv")
};

var testedQueries = new[]
{
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\select_common.sql"),
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\select_array_where.sql"),
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\select_not_filtered.sql"),
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\update_common.sql"),
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\update_array_where.sql"),
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\update_not_filtered.sql"),
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\delete_common.sql"),
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\delete_array_where.sql"),
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\delete_not_filtered.sql"),
    new QueryData("..\\..\\..\\..\\Common\\SqlScripts\\truncate.sql"),
};

var recreateTableAndIndex = new[] { dropTableAndIndex, createTable, createIndex };
var recreateTableAndDropIndex = new[] { dropTableAndIndex, createTable };

var experiments = new List<IExperiment>();

foreach (var insert in inserts)
{
    experiments.Add(
        new RepeatableExperiment(
            new Experiment(recreateTableAndIndex, [insert]), 
            10));
    experiments.Add(
        new RepeatableExperiment(
            new Experiment(recreateTableAndDropIndex, [insert, createIndex]), 
            10));
}

foreach (var query in testedQueries)
{
    experiments.Add(
        new RepeatableExperiment(
            new Experiment(recreateTableAndIndex, [query]),
            10));
}

using var testResultWriter = new StreamWriter("test_result.csv", false);
testResultWriter.WriteLine("test_name;"
    + "common_insert;common_insert_lazy_index;"
    + "package_insert;package_insert_lazy_index;"
    + "bulk_insert;bulk_insert_lazy_index;"
    + "common_select;array_where_select;not_filtered_select;"
    + "common_update;array_where_update;not_filtered_update;"
    + "common_delete;array_where_delete;not_filtered_delete;truncate;");

var experimentsResults = new Dictionary<Settings, IReadOnlyList<Statistic>>();

foreach (var settings in customSettingsList)
{
    var results = await ProcessTestsWithSettings(
        settings, 
        defaultSettings, 
        defaultSettingsRaw, 
        experiments, 
        CancellationToken.None);

    experimentsResults[settings] = results;

    testResultWriter.Write($"{settings.Name};");

    foreach (var result in results)
    {
        testResultWriter.Write($"{result.ClientTimeDuraction.TotalSeconds};");
    }

    testResultWriter.WriteLine();
}

var settingsQueue = new Queue<(Settings, IReadOnlyList<Statistic>)>();

foreach (var settings in customSettingsList[1..])
{
    if (IsFirstStatisticsBiggerThanSecondStatistic(
        experimentsResults.Single(r => r.Key.Name == "").Value,
        experimentsResults[settings]))
    {
        foreach (var settingsForCombine in customSettingsList[1..])
        {
            if (IsFirstStatisticsBiggerThanSecondStatistic(
                    experimentsResults.Single(r => r.Key.Name == "").Value,
                    experimentsResults[settingsForCombine])
                && settings.TryMerge(settingsForCombine, out var combinedSettings)
                && !experimentsResults.ContainsKey(combinedSettings))
            {
                experimentsResults[combinedSettings] = null!;

                var minResult = IsFirstStatisticsBiggerThanSecondStatistic(
                        experimentsResults[settings],
                        experimentsResults[settingsForCombine])
                    ? experimentsResults[settingsForCombine]
                    : experimentsResults[settings];

                settingsQueue.Enqueue((combinedSettings, minResult));
            }
        }
    }
}

while (settingsQueue.Count > 0)
{
    var (settings, parentResults) = settingsQueue.Dequeue();

    var results = await ProcessTestsWithSettings(
        settings,
        defaultSettings,
        defaultSettingsRaw,
        experiments,
        CancellationToken.None);

    experimentsResults[settings] = results;

    testResultWriter.Write($"{settings.Name};");

    foreach (var result in results)
    {
        testResultWriter.Write($"{result.ClientTimeDuraction.TotalSeconds};");
    }

    testResultWriter.WriteLine();

    if (IsFirstStatisticsBiggerThanSecondStatistic(
        parentResults,
        experimentsResults[settings]))
    {
        foreach (var settingsForCombine in customSettingsList[1..])
        {
            if (IsFirstStatisticsBiggerThanSecondStatistic(
                    experimentsResults.Single(r => r.Key.Name == "").Value,
                    experimentsResults[settingsForCombine])
                && settings.TryMerge(settingsForCombine, out var combinedSettings)
                && !experimentsResults.ContainsKey(combinedSettings))
            {
                experimentsResults[combinedSettings] = null!;

                var minResult = IsFirstStatisticsBiggerThanSecondStatistic(
                        experimentsResults[settings],
                        experimentsResults[settingsForCombine])
                    ? experimentsResults[settingsForCombine]
                    : experimentsResults[settings];

                settingsQueue.Enqueue((combinedSettings, minResult));
            }
        }
    }
}

static async Task<IReadOnlyList<Statistic>> ProcessTestsWithSettings(
    Settings settings, 
    Settings defaultSettings, 
    string defaultSettingsRaw, 
    IEnumerable<IExperiment> experiments, 
    CancellationToken token)
{
    Console.WriteLine($"Тест: {settings.Name}");

    var testedSettingsRaw = defaultSettingsRaw;
    foreach (var (settingName, setting) in settings.SettingsWithValues)
    {
        testedSettingsRaw = testedSettingsRaw.Replace(defaultSettings.SettingsWithValues[settingName], setting);
    }

    ApplySettings(testedSettingsRaw);

    return await ExecuteTestsAsync(experiments, CancellationToken.None);
}

async static Task<IReadOnlyList<Statistic>> ExecuteTestsAsync(IEnumerable<IExperiment> experiments, CancellationToken token)
{
    using var connection = new NpgsqlConnection("Host=localhost;Port=5432;Username=postgres;Password=12345;Database=insert_test_database;Pooling=false");
    await connection.OpenAsync();
    var queryFactory = new QueryFactory(connection);
    var results = new List<Statistic>();

    foreach (var experiment in experiments)
    {
        var statistics = await experiment.ExecuteAsync(queryFactory, token);
        results.Add(statistics);
    }

    return results;
}


static void ApplySettings(string settings)
{
    using var confWriter = new StreamWriter("C:\\Program Files\\PostgreSQL\\16\\data\\postgresql.conf", false);
    confWriter.Write(settings);

    ReloadPostgre();
}

static void ReloadPostgre()
{
    var sc = new ServiceController("postgresql-x64-16");
    if (sc.Status == ServiceControllerStatus.Stopped)
    {
        sc.Start();
        while (sc.Status != ServiceControllerStatus.Running)
        {
            Thread.Sleep(100);
            sc.Refresh();
        }
    }
    sc.Stop();
    while (sc.Status != ServiceControllerStatus.Stopped)
    {
        Thread.Sleep(100);
        sc.Refresh();
    }
    sc.Start();
    while (sc.Status != ServiceControllerStatus.Running)
    {
        Thread.Sleep(100);
        sc.Refresh();
    }
}

static bool IsFirstStatisticsBiggerThanSecondStatistic(
    IReadOnlyList<Statistic> firstStatistics, 
    IReadOnlyList<Statistic> secondStatistics)
{
    var differentsSum = 0d;

    for (var i = 0; i < firstStatistics.Count; i++)
    {
        var firstStatistic = firstStatistics[i].ClientTimeDuraction.TotalSeconds;
        var secondStatistic = secondStatistics[i].ClientTimeDuraction.TotalSeconds;

        differentsSum += (firstStatistic - secondStatistic) / firstStatistic;
    }

    return differentsSum > 0; // [TODO] Возможно, нужно отрицательное значение, чтобы поблажку дать
}

class Settings
{
    public IReadOnlyDictionary<string, string> SettingsWithValues { get; }
    public string Name { get; }


    public Settings(IReadOnlyDictionary<string, string> settingsWithValues)
    {
        SettingsWithValues = settingsWithValues 
            ?? throw new ArgumentNullException(nameof(settingsWithValues));

        Name = string.Join(',', settingsWithValues.Values.Order());
    }

    public override bool Equals(object? obj)
    {
        if (obj is not Settings other)
        {
            return false;
        }

        return Name.Equals(other.Name);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public bool TryMerge(Settings other, [NotNullWhen(true)] out Settings newSettings)
    {
        ArgumentNullException.ThrowIfNull(other);

        var newSettingsValues = SettingsWithValues.ToDictionary();

        foreach (var (key, value) in other.SettingsWithValues)
        {
            if (!newSettingsValues.TryGetValue(key, out var intersectionValue))
            {
                newSettingsValues[key] = value;
            }
            else if (value != intersectionValue)
            {
                newSettings = default!;
                return false;
            }
        }

        newSettings = new(newSettingsValues);
        return true;
    }
}