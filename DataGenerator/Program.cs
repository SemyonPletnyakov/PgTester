using System.Globalization;

Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

var rnd = new Random();
Directory.CreateDirectory("..\\..\\..\\..\\Common\\SqlScripts");
using var commonInsertWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\insert_common.sql", false);
using var packageInsertWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\insert_package.sql", false);
using var csvWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\insert_bulk_data.csv", false);

using var commonSelectWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\select_common.sql", false);
using var arrayWhereSelectWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\select_array_where.sql", false);

using var commonUpdateWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\update_common.sql", false);
using var arrayWhereUpdateWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\update_array_where.sql", false);

using var commonDeleteWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\delete_common.sql", false);
using var arrayWhereDeleteWriter = new StreamWriter("..\\..\\..\\..\\Common\\SqlScripts\\delete_array_where.sql", false);

commonInsertWriter.WriteLine("BEGIN;");
packageInsertWriter.WriteLine("BEGIN;");
commonUpdateWriter.WriteLine("BEGIN;");
arrayWhereUpdateWriter.WriteLine("BEGIN;");
commonDeleteWriter.WriteLine("BEGIN;");
arrayWhereDeleteWriter.WriteLine("BEGIN;");

packageInsertWriter.WriteLine($"INSERT INTO orders VALUES");
arrayWhereSelectWriter.Write($"SELECT * from orders WHERE order_id in (");
arrayWhereUpdateWriter.Write($"UPDATE orders SET pick_up_point_id = 128 WHERE order_id in (");
arrayWhereDeleteWriter.Write($"DELETE from orders WHERE order_id in (");
csvWriter.WriteLine("order_id,product_id,user_id,pick_up_point_id,price,status");

var rowsCount = 5_000;
for (var i = 0; i < rowsCount; i++)
{
    var orderId = i;
    var productId = rnd.Next();
    var userId = rnd.Next();
    var pickUpPointId = rnd.Next();
    var price = (decimal)rnd.Next(1,400_000);
    var status = rnd.Next();

    commonInsertWriter.WriteLine($"INSERT INTO orders VALUES ({orderId}, {productId}, {userId}, {pickUpPointId}, {price}, {status});");

    packageInsertWriter.Write($"({orderId}, {productId}, {userId}, {pickUpPointId}, {price}, {status})");

    if (i != rowsCount - 1)
    {
        packageInsertWriter.WriteLine(",");
        arrayWhereSelectWriter.Write($"{orderId},");
        arrayWhereUpdateWriter.Write($"{orderId},");
        arrayWhereDeleteWriter.Write($"{orderId},");
    }
    else
    {
        arrayWhereSelectWriter.WriteLine($"{orderId});");
        arrayWhereUpdateWriter.WriteLine($"{orderId});");
        arrayWhereDeleteWriter.WriteLine($"{orderId});");
        packageInsertWriter.WriteLine(";");
    }

    csvWriter.WriteLine($"{orderId},{productId},{userId},{pickUpPointId},{price},{status}");

    commonSelectWriter.WriteLine($"SELECT * from orders WHERE order_id = {orderId};");
    commonUpdateWriter.WriteLine($"UPDATE orders SET pick_up_point_id = 128 WHERE order_id = {orderId};");
    commonDeleteWriter.WriteLine($"DELETE from orders WHERE order_id = {orderId};");
}

commonInsertWriter.WriteLine("COMMIT;");
packageInsertWriter.WriteLine("COMMIT;");
commonUpdateWriter.WriteLine("COMMIT;");
arrayWhereUpdateWriter.WriteLine("COMMIT;");
commonDeleteWriter.WriteLine("COMMIT;");
arrayWhereDeleteWriter.WriteLine("COMMIT;");