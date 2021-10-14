using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Kafka7;
using System.Diagnostics;

var bootstrapServers = Environment.GetEnvironmentVariable("K7_BS") ?? "none:9092";
var click = Environment.GetEnvironmentVariable("K7_CLICK") ?? "click:8123";
var topic = Environment.GetEnvironmentVariable("K7_TOPIC") ?? "new1";
var dop = Convert.ToInt32(Environment.GetEnvironmentVariable("K7_DOP") ?? "100");
var payloadSize = Convert.ToInt32(Environment.GetEnvironmentVariable("K7_PAYLOAD") ?? "1000");
var rf = Convert.ToInt16(Environment.GetEnvironmentVariable("K7_RF") ?? "1");

Console.WriteLine($"TOPIC\t{topic}");
Console.WriteLine($"RF\t{rf}");
Console.WriteLine($"DOP\t{dop}");
Console.WriteLine($"PAYLOAD\t{payloadSize}");

//var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = "kfk-0.broker.kt.svc.cluster.local:9092" }).Build();
var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
var meta = admin.GetMetadata(Timeout.InfiniteTimeSpan);
var brokers = string.Join(",", meta.Brokers.Select(b => b.Host + ":" + b.Port));
Console.WriteLine(brokers.Replace(",", Environment.NewLine));
if (!meta.Topics.Any(t => t.Topic == topic))
    await admin.CreateTopicsAsync(new[] { new TopicSpecification { Name = topic, NumPartitions = meta.Brokers.Count, ReplicationFactor = rf } });

meta = admin.GetMetadata(Timeout.InfiniteTimeSpan);

meta.Topics.Where(t => t.Topic == topic).First().Partitions.ForEach(p =>
{
    Console.Write(p.PartitionId);
    Console.Write("|");
    foreach (var r in p.Replicas) Console.Write("/" + r + " ");
    Console.WriteLine();
});

//var cw = new ClickhouseWriter("Host=click.kt.svc.cluster.local", "default.lat");
var cw = new ClickhouseWriter($"Host={click}", "default.lat");

var config = new ProducerConfig { BootstrapServers = brokers, Acks=Acks.All };
using var producer = new ProducerBuilder<string, string>(config).Build();

var sem = new SemaphoreSlim(dop); // limited concurrency

while (true)
{
    await sem.WaitAsync();
    var m = new Mes { Payload = Mes.CreatePayload(payloadSize) };
    var msg = new Message<string, string> { Key = m.Id.ToString(), Value = m.ToJsonString() };
    var start = DateTime.Now;
    _ = producer
        .ProduceAsync(topic, msg)
        .ContinueWith(t =>
        {
            var mes = t.Result.Value.FromJsonString<Mes>();
            var latency = DateTime.Now - mes.Start;
            cw.Write(DateTime.Now, "PRD", latency.TotalMilliseconds, 1);
            sem.Release();
        });
}

