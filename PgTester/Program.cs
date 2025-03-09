using Npgsql;
using System.Diagnostics;
using System.ServiceProcess;

var defaultSettings = new string[]
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
}.ToDictionary(str => str.Split(' ')[0], str => str);

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
        settings.ToDictionary(str => str.Split(' ')[0], str => str))
    .ToArray();

using var defaultSettingsReader = new StreamReader("..\\..\\..\\..\\Common\\postgresql.conf");
var defaultSettingsRaw = defaultSettingsReader.ReadToEnd();

using var testResultWriter = new StreamWriter("test_result.csv", false);
testResultWriter.WriteLine("test_name;"
    + "common_insert;common_insert_lazy_index;"
    + "package_insert;package_insert_lazy_index;"
    + "bulk_insert;bulk_insert_lazy_index;"
    + "common_select;array_where_select;not_filtered_select;"
    + "common_update;array_where_update;not_filtered_update;"
    + "common_delete;array_where_delete;not_filtered_delete;truncate;");

foreach (var settings in customSettingsList)
{
    var testName = string.Join(',', settings.Values);
    if (testName == "")
    {
        testName = "Базовые настройки";
    }

    Console.WriteLine($"Тест: {testName}");

    var testedSettingsRaw = defaultSettingsRaw;
    foreach (var (settingName, setting) in settings)
    {
        testedSettingsRaw = testedSettingsRaw.Replace(defaultSettings[settingName], setting);
    }
    using var confWriter = new StreamWriter("C:\\Program Files\\PostgreSQL\\16\\data\\postgresql.conf", false);
    confWriter.Write(testedSettingsRaw);

    ReloadPostgre();

    await ExecuteTestsAndWriteResults(testName, testResultWriter);
}

async Task ExecuteTestsAndWriteResults(string testName, StreamWriter testResultWriter)
{
    testResultWriter.Write($"{testName};");
    using var connection = new NpgsqlConnection("Host=localhost;Port=5432;Username=postgres;Password=12345;Database=insert_test_database;Pooling=false");
    await connection.OpenAsync();

    var time = await ExecuteQueryFromFileWithCreateTableAndIndex("insert\\common_insert.sql", connection);
    testResultWriter.Write($"{time.TotalSeconds};");
    time = await ExecuteQueryFromFileWithCreateTableAndLazyIndex("insert\\common_insert.sql", connection);
    testResultWriter.Write($"{time.TotalSeconds};");
    time = await ExecuteQueryFromFileWithCreateTableAndIndex("insert\\package_insert.sql", connection);
    testResultWriter.Write($"{time.TotalSeconds};");
    time = await ExecuteQueryFromFileWithCreateTableAndLazyIndex("insert\\package_insert.sql", connection);
    testResultWriter.Write($"{time.TotalSeconds};");
    time = await ExecuteBulkInsertWithCreateTableAndIndex(connection);
    testResultWriter.Write($"{time.TotalSeconds};");
    time = await ExecuteBulkInsertWithCreateTableAndLazyIndex(connection);
    testResultWriter.Write($"{time.TotalSeconds};");

    var operationsForPreparedData = new string[]
    {
        "select\\common_select.sql",
        "select\\array_where_select.sql",
        "select\\not_filtered_select.sql",
        "update\\common_update.sql",
        "update\\array_where_update.sql",
        "update\\not_filtered_update.sql",
        "delete\\common_delete.sql",
        "delete\\array_where_delete.sql",
        "delete\\not_filtered_delete.sql",
        "delete\\truncate.sql",
    };

    foreach (var localPath in operationsForPreparedData)
    {
        time = await ExecuteQueryFromFileWithCreateTableAndIndexAndPreparedData(localPath, connection);
        testResultWriter.Write($"{time.TotalSeconds};");
    }

    testResultWriter.WriteLine();
}
async Task<TimeSpan> ExecuteQueryFromFileWithCreateTableAndIndex(string localPath, NpgsqlConnection connection)
{
    await ExecuteQueryFromFileAsync("recreate_table.sql", connection);
    await ExecuteQueryFromFileAsync("create_index.sql", connection);
    return await ExecuteQueryFromFileAsync(localPath, connection);
}

async Task<TimeSpan> ExecuteQueryFromFileWithCreateTableAndLazyIndex(string localPath, NpgsqlConnection connection)
{
    await ExecuteQueryFromFileAsync("recreate_table.sql", connection);
    return await ExecuteQueryFromFileAsync(localPath, connection) + await ExecuteQueryFromFileAsync("create_index.sql", connection);
}

async Task<TimeSpan> ExecuteBulkInsertWithCreateTableAndIndex(NpgsqlConnection connection)
{
    await ExecuteQueryFromFileAsync("recreate_table.sql", connection);
    await ExecuteQueryFromFileAsync("create_index.sql", connection);
    return await ExecuteBulkInsert(connection);
}

async Task<TimeSpan> ExecuteBulkInsertWithCreateTableAndLazyIndex(NpgsqlConnection connection)
{
    await ExecuteQueryFromFileAsync("recreate_table.sql", connection);
    return await ExecuteBulkInsert(connection) + await ExecuteQueryFromFileAsync("create_index.sql", connection); ;
}

async Task<TimeSpan> ExecuteQueryFromFileWithCreateTableAndIndexAndPreparedData(string localPath, NpgsqlConnection connection)
{
    await ExecuteBulkInsertWithCreateTableAndLazyIndex(connection);
    return await ExecuteQueryFromFileAsync(localPath, connection);
}

async Task<TimeSpan> ExecuteQueryFromFileAsync(string localPath, NpgsqlConnection connection)
{
    if (localPath is not "recreate_table.sql" and not "create_index.sql")
    {
        await Console.Out.WriteLineAsync(localPath);
    }
    using var reader = new StreamReader("..\\..\\..\\..\\Common\\SqlScripts\\" + localPath);
    var sqlExpression = reader.ReadToEnd();
    var stopWatch = new Stopwatch();
    stopWatch.Start();
    using var command = new NpgsqlCommand(sqlExpression, connection);
    command.CommandTimeout = 1_000_000;
    await command.ExecuteNonQueryAsync();
    stopWatch.Stop();
    return stopWatch.Elapsed;
}

async Task<TimeSpan> ExecuteBulkInsert(NpgsqlConnection connection)
{
    using var expressionReader = new StreamReader("..\\..\\..\\..\\Common\\SqlScripts\\insert\\bulk_insert.sql");
    var sqlExpression = expressionReader.ReadToEnd();
    using var dataReader = new StreamReader("..\\..\\..\\..\\Common\\SqlScripts\\insert\\insert_data.csv");
    var csv = dataReader.ReadToEnd();
    var stopWatch = new Stopwatch();
    stopWatch.Start();
    using var writer = connection.BeginTextImport(sqlExpression);
    await writer.WriteAsync(csv);
    stopWatch.Stop();
    return stopWatch.Elapsed;
}

void ReloadPostgre()
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