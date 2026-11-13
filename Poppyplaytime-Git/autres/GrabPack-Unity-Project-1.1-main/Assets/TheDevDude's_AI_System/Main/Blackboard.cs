using System;
using System.Collections.Generic;

public class Blackboard
{
    private readonly Dictionary<string, object> _data = new();


    public void Set<T>(string key, T value)
    {
        _data[key] = value;
    }

    public bool TryGet<T>(string key, out T value)
    {
        if (_data.TryGetValue(key, out object stored) && stored is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }


    public T Get<T>(string key, T defaultValue = default)
    {
        return TryGet(key, out T value) ? value : defaultValue;
    }


    public bool Has(string key)
    {
        return _data.ContainsKey(key);
    }


    public bool Remove(string key)
    {
        return _data.Remove(key);
    }


    public void Clear()
    {
        _data.Clear();
    }


    public IEnumerable<string> Keys => _data.Keys;

    public int Count => _data.Count;
}