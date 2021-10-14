using ClickHouse.Client.ADO;
using ClickHouse.Client.Copy;

namespace Kafka7
{
    public class ClickhouseWriter
    {
        List<object[]> rows = new();
        readonly string connStr;
        readonly string table;
        public ClickhouseWriter(string connStr, string table)
        {
            this.table = table;
            this.connStr = connStr;
            Task.Factory.StartNew(async () => {
                while (true)
                {
                    try { await Worker(); }
                    catch (Exception e)
                    {
                        //Console.WriteLine(e.ToString());
                        Console.WriteLine(e.Message);
                    }
                    await Task.Delay(1000);
                }
            }, TaskCreationOptions.LongRunning);
        }

        async Task Worker()
        {
            while (true)
            {
                await using var c = new ClickHouseConnection(connStr);
                await c.OpenAsync();

                using var bulkCopyInterface = new ClickHouseBulkCopy(c)
                {
                    DestinationTableName = table,
                    BatchSize = 100000
                };


                while (true)
                {
                    await Task.Delay(333);
                    if (rows.Count == 0) continue;

                    List<object[]> batch;
                    lock (this)
                    {
                        if (rows.Count == 0) continue;
                        batch = rows;
                        rows = new List<object[]>();
                    }

                    await bulkCopyInterface.WriteToServerAsync(batch);
                }
            }
        }
        public void Write(params object[] row)
        {
            lock (this) rows.Add(row);
        }
    }

    public struct ConcurrencyDescriptor
    {
        public int Put { get; set; }
        public int Get { get; set; }
    }
}
