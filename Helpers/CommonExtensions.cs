using System.Collections;
using System.Collections.Specialized;
using System.Text;

namespace S3WebApi.Helpers;

public static class CommonExtensions
{
    public static string GetDictionaryValue(this Dictionary<string, object> keyValues, string key)
    {
        if (keyValues.TryGetValue(key, out object value))
        {
            return value?.ToString();
        }
        return null;
    }

    public static string ResolveContextUser(this string user)
    {
        return user.ToLower().Contains("src_sys_cd_") ? "SharePoint App" : user;
    }

    /// <summary>
    /// Truncate the string to the given length limit.  (If the string is shorter than the limit, or null, the original string is returned).
    /// </summary>
    /// <param name="lengthLimit">The maximum number of characters to return.</param>
    /// <returns>The original string, or a truncated version of it.</returns>
    public static string Truncate(this string longString, int lengthLimit)
    {
        if (longString?.Length >= lengthLimit)
        {
            return longString.Substring(0, lengthLimit);
        }

        return longString;
    }

    /// <summary>
    /// Combines the two <see cref="IEnumerable{T}"/>s into one.
    /// </summary>
    /// <param name="firstEnumerable"></param>
    /// <param name="secondEnumerable"></param>
    /// <returns>One enumerable consisting of the items of the first <see cref="IEnumerable{T}"/>, followed by the items of the second <see cref="IEnumerable{T}"/>.</returns>
    public static IEnumerable<T> AndThen<T>(this IEnumerable<T> firstEnumerable, IEnumerable<T> secondEnumerable)
    {
        foreach (var item in firstEnumerable)
        {
            yield return item;
        }
        foreach (var item in secondEnumerable)
        {
            yield return item;
        }
    }

    /// <summary>
    /// Adds an item to start of an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="firstItem">The first item in the returned enumerable.</param>
    /// <param name="secondEnumerable">The second and subsequent items in the returned enumerable. </param>
    /// <returns>One enumerable consisting of the given item, followed by the items of the <see cref="IEnumerable{T}"/>.</returns>
    public static IEnumerable<T> AndThen<T>(this T firstItem, IEnumerable<T> secondEnumerable)
    {
        yield return firstItem;
        foreach (var item in secondEnumerable)
        {
            yield return item;
        }
    }

    /// <summary>
    /// Appends an item to the end of an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <param name="enumerable">The enumerable that comes before the item.</param>
    /// <param name="nextItem">The item to be enumerated after the target enumerable is empty.</param>
    /// <returns>One enumerable consisting of the items of the first <see cref="IEnumerable{T}"/>, followed by the given item.</returns>
    public static IEnumerable<T> AndThen<T>(this IEnumerable<T> enumerable, T nextItem)
    {
        foreach (var item in enumerable)
        {
            yield return item;
        }
        yield return nextItem;
    }

    /// <summary>
    /// Add the values to the list, returning the enlarged list.
    /// </summary>
    /// <param name="baseList">The list to which values will be added.</param>
    /// <param name="valuesToAdd">The values to add to the list.</param>
    /// <returns>The newly enlarged baseList, to allow for fluid calls.</returns>
    public static List<T> AddAll<T>(this List<T> baseList, IEnumerable<T> valuesToAdd)
    {
        if (baseList == null)
        {
            baseList = new List<T>();
        }
        baseList.AddRange(valuesToAdd);
        return baseList;
    }

    /// <summary>
    /// Iterate over the items executing the action for each one.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="items">The items to perform the action on.</param>
    /// <param name="action">The action to perform for each item.</param>
    public static void ForEach<T>(this IEnumerable<T> items, Action<T> action)
    {
        if (action == null)
        {
            throw new ArgumentException("ForEach action is null", nameof(action));
        }
        foreach (var item in items)
        {
            action.Invoke(item);
        }
    }

    /// <summary>
    /// Safely append extra path elements to a Uri.  Empty or whitespace path elements are ignored.
    /// </summary>
    public static Uri AppendPath(this Uri uri, params string[] paths)
    {
        return uri.IsAbsoluteUri
                        ? new Uri(paths.SkipNullOrWhiteSpace().Aggregate(uri.AbsoluteUri, (current, path) => $"{current.TrimEnd('/')}/{path.TrimStart('/')}"))
                        : new Uri(paths.SkipNullOrWhiteSpace().Aggregate(uri.ToString(), (current, path) => $"{current.TrimEnd('/')}/{path.TrimStart('/')}"), UriKind.Relative);
    }

    /// <summary>
    /// Skips over elements in a string enumeration for which <see cref="string.IsNullOrWhiteSpace"/> return true.
    /// </summary>
    /// <returns>The same enumeration, but with the nulls removed.</returns>
    public static IEnumerable<string> SkipNullOrWhiteSpace(this IEnumerable<string> source)
    {
        foreach (var element in source)
        {
            if (element != null && !string.IsNullOrWhiteSpace(element))
            {
                yield return element;
            }
        }
    }

    public static IEnumerable<T> SkipNulls<T>(this IEnumerable<T?> source)
    {
        foreach (var element in source)
        {
            if (element == null)
            {
                continue;
            }
            yield return element;
        }
    }

    /// <summary>
    /// Transforms a collection into a collection of batches of given size.
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items, int maxItems)
    {
        return items.Select((item, inx) => new { item, inx })
                    .GroupBy(x => x.inx / maxItems)
                    .Select(g => g.Select(x => x.item));
    }

    /// <summary>
    /// Takes an enumerable object and the separator you want to use and builds a 
    /// concatenated string using the supplied separator.
    /// Enumerable cannot be NULL.
    /// Uses object.ToString() when describing each object.
    /// </summary>
    /// <param name="enumerable">Enumerable to go through and build from.</param>
    /// <param name="separator">Nullable string to separate each item in the concatenation with.</param>
    /// <returns>Empty string if nothing in the list, otherwise the concatenation.</returns>
    public static string ToString(this IEnumerable enumerable, string separator)
    {
        return enumerable.ToString(separator, null);
    }

    /// <summary>
    /// Takes an enumerable object and the separator you want to use and builds a 
    /// concatenated string using the supplied separator.
    /// Enumerable cannot be NULL.
    /// Uses object.ToString() when describing each object.
    /// </summary>
    /// <param name="enumerable">Enumerable to go through and build from.</param>
    /// <param name="separator">Nullable string to separate each item in the concatenation with.</param>
    /// <param name="lastItemSeparator">Separator between the last two items, e.g. the "and" in "1, 2 and 3".</param>
    /// <returns>Empty string if nothing in the list, otherwise the concatenation.</returns>
    public static string ToString(this IEnumerable enumerable, string separator, string lastItemSeparator)
    {
        return ToString<object>(enumerable, separator, lastItemSeparator, o => o.ToString());
    }

    /// <summary>
    /// An overloaded version of the base, which lets you define the anonymous delegate that will describe your objects "T".
    /// </summary>
    /// <typeparam name="T">Class of the items within the enumerable.</typeparam>
    /// <param name="enumerable">Enumerable to go through and build from.</param>
    /// <param name="separator">Nullable string to separate each item in the concatenation with.</param>
    /// <param name="delegateToString">Anonymous delegate implementation that returns the string to use when building the concatenation.</param>
    /// <returns>Empty string if nothing in the list, otherwise the concatenation.</returns>
    public static string ToString<T>(this IEnumerable enumerable, string separator, Func<T, string> delegateToString)
    {
        return enumerable.ToString(separator, null, delegateToString);
    }

    /// <summary>
    /// An overloaded version of the base, which lets you define the anonymous delegate that will describe your objects "T".
    /// </summary>
    /// <typeparam name="T">Class of the items within the enumerable.</typeparam>
    /// <param name="enumerable">Enumerable to go through and build from.</param>
    /// <param name="separator">Nullable string to separate each item in the concatenation with.</param>
    /// <param name="lastItemSeparator">Separator between the last two items, e.g. the "and" in "1, 2 and 3".</param>
    /// <param name="delegateToString">Anonymous delegate implementation that returns the string to use when building the concatenation.</param>
    /// <returns>Empty string if nothing in the list, otherwise the concatenation.</returns>
    public static string ToString<T>(this IEnumerable enumerable, string separator, string lastItemSeparator, Func<T, string> delegateToString)
    {
        enumerable.RequireNotNull(nameof(enumerable));
        delegateToString.RequireNotNull(nameof(delegateToString));

        // be null safe with the collection.
        separator = separator ?? string.Empty;

        // The information required to determine when to apply the last item separator.
        int numItems = enumerable.Cast<T>().Count();
        bool allowDifferentLastSeparator = numItems > 1;
        int secondLastIndex = numItems - 2;

        StringBuilder sb = new StringBuilder();

        int index = 0;
        foreach (T element in enumerable)
        {
            string currentSeparator = (lastItemSeparator != null && allowDifferentLastSeparator && index == secondLastIndex) ? lastItemSeparator : separator;
            sb.AppendFormat("{0}{1}", delegateToString(element), currentSeparator);
            index++;
        }

        if (sb.Length > 0)
        {
            if (separator.Length > 0)
            {
                sb.Remove(sb.Length - separator.Length, separator.Length);
            }

            return sb.ToString();
        }

        return string.Empty;
    }

    public static StringCollection ToStringCollection(this IEnumerable<string> strings)
    {
        strings.RequireNotNull(nameof(strings));

        var stringCollection = new StringCollection();
        foreach (string item in strings)
        {
            stringCollection.Add(item);
        }

        return stringCollection;
    }

    public static bool IsDocumentTypeNotAllowedToArchived(string documentUrl, string Extensions)
    {
        if (string.IsNullOrWhiteSpace(documentUrl))
        {
            // If the URL is null or empty, you can decide to allow or disallow.
            // Here, we disallow archiving.
            return false;
        }
        string[] disallowedExtensions = Extensions.Split(",");
        // Extract the file extension from the URL
        string extension = Path.GetExtension(documentUrl);

        // Check if the extension is ".aspx" (case-insensitive)
        if (!string.IsNullOrEmpty(extension) &&
         Array.Exists(disallowedExtensions, ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        return false;
    }
}
