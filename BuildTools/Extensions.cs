namespace JScr.BuildTools;

internal static class DictionaryExtensions
{
    public static bool TryGetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey[] keys, out TValue? value) where TKey : notnull
    {
        foreach (var key in keys)
        {
            if (dict.TryGetValue(key, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }
}