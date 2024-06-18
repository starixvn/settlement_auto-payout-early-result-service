namespace AutoPayout.Helpers;
public static class ObjectExtensionMethods
{
    public static void AddIf<T>(this List<T> self, bool conditionValue, T source)
    {
        if (conditionValue)
        {
            self.Add(source);
        }
    }

    public static bool TryGetValue<T>(this IEnumerable<T> self, Func<T, bool> predicate, out T value)
    {
        value = self.FirstOrDefault(predicate);

        return value is not null;
    }
}
