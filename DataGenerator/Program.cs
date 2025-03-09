using System.Globalization;
using System.Reflection;

Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

var rnd = new Random();
using var commonInsertWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\insert\\common_insert.sql", false);
using var packageInsertWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\insert\\package_insert.sql", false);
using var csvWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\insert\\insert_data.csv", false);

using var commonUpdateWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\update\\common_update.sql", false);
using var arrayWhereUpdateWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\update\\array_where_update.sql", false);

using var commonDeleteWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\delete\\common_delete.sql", false);
using var arrayWhereDeleteWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\delete\\array_where_delete.sql", false);

using var commonSelectWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\select\\common_select.sql", false);
using var arrayWhereSelectWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\select\\array_where_select.sql", false);

commonInsertWriter.WriteLine("BEGIN;");
packageInsertWriter.WriteLine("BEGIN;");
commonUpdateWriter.WriteLine("BEGIN;");
arrayWhereUpdateWriter.WriteLine("BEGIN;");
commonDeleteWriter.WriteLine("BEGIN;");
arrayWhereDeleteWriter.WriteLine("BEGIN;");

packageInsertWriter.WriteLine($"INSERT INTO test_table VALUES");
arrayWhereUpdateWriter.Write($"UPDATE test_table SET param_1 = 128 WHERE id in (");
arrayWhereDeleteWriter.Write($"DELETE from test_table WHERE id in (");
arrayWhereSelectWriter.Write($"SELECT * from test_table WHERE id in (");
csvWriter.WriteLine("id,param_1,param_2,param_3,param_4,param_5,param_6");

var rowsCount = 1_000;
for (var i = 0; i < rowsCount; i++)
{
    var id = i;
    var param1 = rnd.Next();
    var param2 = rnd.Next();
    var param3 = rnd.Next();
    var param4 = rnd.NextDouble();
    var param5 = rnd.NextDouble();
    var param6 = rnd.NextDouble();

    commonInsertWriter.WriteLine($"INSERT INTO test_table VALUES ({id}, {param1}, {param2}, {param3}, {param4}, {param5}, {param6});");

    packageInsertWriter.Write($"({id}, {param1}, {param2}, {param3}, {param4}, {param5}, {param6})");

    if (i != rowsCount - 1)
    {
        packageInsertWriter.WriteLine(",");
        arrayWhereUpdateWriter.Write($"{id},");
        arrayWhereDeleteWriter.Write($"{id},");
        arrayWhereSelectWriter.Write($"{id},");
    }
    else
    {
        arrayWhereUpdateWriter.WriteLine($"{id});");
        arrayWhereDeleteWriter.WriteLine($"{id});");
        arrayWhereSelectWriter.WriteLine($"{id});");
        packageInsertWriter.WriteLine(";");
    }

    csvWriter.WriteLine($"{id},{param1},{param2},{param3},{param4},{param5},{param6}");

    commonUpdateWriter.WriteLine($"UPDATE test_table SET param_1 = 128 WHERE id = {id};");
    commonDeleteWriter.WriteLine($"DELETE from test_table WHERE id = {id};");
    commonSelectWriter.WriteLine($"SELECT * from test_table WHERE id = {id};");
}

commonInsertWriter.WriteLine("COMMIT;");
packageInsertWriter.WriteLine("COMMIT;");
commonUpdateWriter.WriteLine("COMMIT;");
arrayWhereUpdateWriter.WriteLine("COMMIT;");
commonDeleteWriter.WriteLine("COMMIT;");
arrayWhereDeleteWriter.WriteLine("COMMIT;");