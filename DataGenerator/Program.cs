using System.Globalization;

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

packageInsertWriter.WriteLine($"INSERT INTO orders VALUES");
arrayWhereUpdateWriter.Write($"UPDATE orders SET pick_up_point_id = 128 WHERE order_id in (");
arrayWhereDeleteWriter.Write($"DELETE from orders WHERE order_id in (");
arrayWhereSelectWriter.Write($"SELECT * from orders WHERE order_id in (");
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
        arrayWhereUpdateWriter.Write($"{orderId},");
        arrayWhereDeleteWriter.Write($"{orderId},");
        arrayWhereSelectWriter.Write($"{orderId},");
    }
    else
    {
        arrayWhereUpdateWriter.WriteLine($"{orderId});");
        arrayWhereDeleteWriter.WriteLine($"{orderId});");
        arrayWhereSelectWriter.WriteLine($"{orderId});");
        packageInsertWriter.WriteLine(";");
    }

    csvWriter.WriteLine($"{orderId},{productId},{userId},{pickUpPointId},{price},{status}");

    commonUpdateWriter.WriteLine($"UPDATE orders SET pick_up_point_id = 128 WHERE order_id = {orderId};");
    commonDeleteWriter.WriteLine($"DELETE from orders WHERE order_id = {orderId};");
    commonSelectWriter.WriteLine($"SELECT * from orders WHERE order_id = {orderId};");
}

commonInsertWriter.WriteLine("COMMIT;");
packageInsertWriter.WriteLine("COMMIT;");
commonUpdateWriter.WriteLine("COMMIT;");
arrayWhereUpdateWriter.WriteLine("COMMIT;");
commonDeleteWriter.WriteLine("COMMIT;");
arrayWhereDeleteWriter.WriteLine("COMMIT;");