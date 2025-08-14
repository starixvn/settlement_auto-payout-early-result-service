using System.Text.Json;

namespace AutoPayout.Extensions;

public static class ListExtensions
{
    public static List<T> DeepClone<T>(this IEnumerable<T> source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<List<T>>(json);
    }
}