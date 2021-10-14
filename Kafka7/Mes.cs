using System.Buffers;
using System.Text.Json;

namespace Kafka7
{
    public class Mes
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? Payload { get; set; }
        public DateTime Start { get; set; } = DateTime.Now;

        static Random rnd = new();
        public static string CreatePayload(int size)
        {
            var x = Math.Max(10, (int)(size * 0.75));
            using var buf = MemoryPool<byte>.Shared.Rent(x);
            rnd.NextBytes(buf.Memory.Span[..x]);
            return Convert.ToBase64String(buf.Memory.Span[..x]);
        }
    }

    public static class JsonExt
    {
        public static string ToJsonString(this object v) =>
            JsonSerializer.Serialize(v);
        public static T? FromJsonString<T>(this string j) =>
            JsonSerializer.Deserialize<T>(j);
    }
}
