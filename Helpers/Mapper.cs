using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class Mapper
{
    public static T MapToClass<T>(Dictionary<string, object> dict) where T : new()
    {
        T obj = new T();
        var type = typeof(T);

        foreach (var kvp in dict)
        {
            var prop = type.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && kvp.Value != DBNull.Value)
            {
                try
                {
                    var convertedValue = Convert.ChangeType(kvp.Value, prop.PropertyType);
                    prop.SetValue(obj, convertedValue);
                }
                catch
                {
                    // Optional: log or handle conversion errors
                }
            }
        }
        return obj;
    }

    public static async Task<List<T>> GetTypeObjectList<T>(IEnumerable<dynamic> result) where T : new()
    {
        try
        {
            var resultList = new List<Dictionary<string, object>>();
            if (result.Any() && result.Count() > 0)
            {
                foreach (var row in result)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in row)
                    {
                        dict[prop.Key] = prop.Value;
                    }
                    resultList.Add(dict);
                }
            }
            if (result.Any())
            {
                var typedResult = result
                    .Cast<IDictionary<string, object>>() // works for DapperRow
                    .Select(dict => Mapper.MapToClass<T>(dict.ToDictionary(k => k.Key, v => v.Value)))
                    .ToList();
                return typedResult;
            }
        }
        catch (System.Exception ex)
        {
        }
        return default;
    }
}