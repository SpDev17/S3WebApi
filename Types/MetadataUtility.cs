using System.Linq;
using System.Reflection;
using S3WebApi.Helpers;
using S3WebApi.Models;
using Validation;

namespace S3WebApi.Types;

public class MetadataUtility
{
    public static IEnumerable<string> GetFields<T>() where T : class, IListItemEntity
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var list = new List<string>();

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var propertyInfo in properties)
        {
            var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

            if (ignore != null)
            {
                continue;
            }

            var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();
            var inherited = columnBinding?.Inherited ?? false;
            var excludeFromClientMapping = columnBinding?.ExcludeInClientFieldMapping ?? false;
            var isIncludedByDefault = columnBinding?.IsIncludedByDefault ?? false;

            if (inherited || excludeFromClientMapping || isIncludedByDefault)
            {
                continue;
            }

            list.Add(propertyInfo.Name);
        }

        return list;
    }

    public static bool AreFieldsValid<T>(IReadOnlyCollection<string>? additionalFields) where T : class, IListItemEntity
    {
        if (additionalFields == null || !additionalFields.Any())
        {
            return true;
        }

        var validFields = GetFields<T>();

        return additionalFields.All(field => validFields.Any(validField => string.Equals(validField, field, StringComparison.OrdinalIgnoreCase)));
    }

    public static string ExpandFields<T>(
        IConfiguration configuration,
        string library,
        ICollection<string>? additionalFields) where T : class, IListItemEntity
    {
        var fieldsList = LibraryDefaultFields.GetDefaultFields(configuration, library).ToList();

        if (additionalFields != null && additionalFields.Any())
        {
            fieldsList.AddRange(additionalFields.Distinct(StringComparer.OrdinalIgnoreCase));
        }

        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var list = new List<string>();

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var propertyInfo in properties)
        {
            var propertyName = propertyInfo.Name;

            if (!fieldsList.Any(field => string.Equals(field, propertyName, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();
            var name = columnBinding?.Name ?? propertyInfo.Name;

            list.Add(name);
        }

        const string expandFieldsFormat = "fields($select={0})";
        const string fieldsSeparator = ",";

        return string.Format(expandFieldsFormat, list.ToString(fieldsSeparator));
    }

    public static T BindProperties<T>(
        T model,
        Dictionary<string, Tuple<string, object>> data,
        Func<string, string, string, string?, Guid> termsPredicate) where T : class, IDocumentModel
    {
        Requires.NotNull(model, nameof(model));

        var type = model.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var propertyInfo in properties)
        {
            var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

            if (ignore != null)
            {
                continue;
            }

            var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();
            var name = columnBinding?.Name ?? propertyInfo.Name;
            var readOnly = columnBinding?.ReadOnly ?? false;
            var inherited = columnBinding?.Inherited ?? false;

            if (readOnly || inherited)
            {
                continue;
            }

            if (!data.TryGetValue(name, out var value))
            {
                continue;
            }

            var (_, fieldValue) = value;
            var bindingType = ListItemBinder.GetBindingType(propertyInfo);

            switch (bindingType)
            {
                case ListItemBindingType.Undefined:
                    break;

                case ListItemBindingType.String:
                    propertyInfo.SetValue(model, Convert.ToString(fieldValue));
                    break;

                case ListItemBindingType.Integer:
                    propertyInfo.SetValue(model, Convert.ToInt32(fieldValue));
                    break;

                case ListItemBindingType.Double:
                    propertyInfo.SetValue(model, Convert.ToDouble(fieldValue));
                    break;

                case ListItemBindingType.Boolean:
                    propertyInfo.SetValue(model, Convert.ToBoolean(fieldValue));
                    break;

                case ListItemBindingType.DateTime:
                    propertyInfo.SetValue(model, DateTime.Parse(Convert.ToString(fieldValue)!));
                    break;

                case ListItemBindingType.Choice:
                    throw new NotSupportedException();

                case ListItemBindingType.MultipleChoice:
                    throw new NotSupportedException();

                case ListItemBindingType.Lookup:
                case ListItemBindingType.MultipleLookup:

                    // Do not throw an exception here!
                    // Any incoming values for lookup and multi-lookup types will
                    // be handled by 'BindLookup'

                    break;

                case ListItemBindingType.ManagedMetadata:
                    propertyInfo.SetValue(model, GetManagedMetadata(model, Convert.ToString(fieldValue)!, name, termsPredicate));
                    break;

                case ListItemBindingType.MultipleManageMetadata:

                    var mmmdValue = Convert.ToString(fieldValue)!;
                    var mmmdList = mmmdValue.Split(',', StringSplitOptions.RemoveEmptyEntries);

                    var mmmd = mmmdList
                        .Select(item => GetManagedMetadata(model, item, name, termsPredicate))
                        .Where(mmd => mmd != null)
                        .ToList();

                    if (mmmd.Any())
                    {
                        propertyInfo.SetValue(model, mmmd);
                    }

                    break;

                case ListItemBindingType.Hyperlink:
                    break;

                case ListItemBindingType.DocumentId:
                    throw new InvalidOperationException();

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return model;
    }

    public static T BindLookups<T>(
        T model,
        Dictionary<string, IReadOnlyCollection<int>> lookups) where T : class, IDocumentModel
    {
        Requires.NotNull(model, nameof(model));

        var type = model.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var propertyInfo in properties)
        {
            var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

            if (ignore != null)
            {
                continue;
            }

            var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();
            var name = columnBinding?.Name ?? propertyInfo.Name;
            var readOnly = columnBinding?.ReadOnly ?? false;
            var inherited = columnBinding?.Inherited ?? false;

            if (readOnly || inherited)
            {
                continue;
            }

            if (!lookups.TryGetValue(name, out var lookupList))
            {
                continue;
            }

            var bindingType = ListItemBinder.GetBindingType(propertyInfo);

            switch (bindingType)
            {
                case ListItemBindingType.Undefined:
                case ListItemBindingType.String:
                case ListItemBindingType.Integer:
                case ListItemBindingType.Double:
                case ListItemBindingType.Boolean:
                case ListItemBindingType.DateTime:
                case ListItemBindingType.ManagedMetadata:
                case ListItemBindingType.MultipleManageMetadata:
                case ListItemBindingType.Hyperlink:
                case ListItemBindingType.Choice:
                case ListItemBindingType.MultipleChoice:
                case ListItemBindingType.DocumentId:
                    throw new InvalidOperationException("Invalid property bindings");

                case ListItemBindingType.Lookup:

                    if (lookupList.Any())
                    {
                        propertyInfo.SetValue(model, new Lookup(lookupList.First()));
                    }

                    break;

                case ListItemBindingType.MultipleLookup:

                    if (lookupList.Any())
                    {
                        var multipleLookups =
                            from int lookup in lookupList select new Lookup(lookup);

                        propertyInfo.SetValue(model, multipleLookups.ToList());
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return model;
    }

    private static ManagedMetadata? GetManagedMetadata<T>(
        T model,
        string value,
        string columnName,
        Func<string, string, string, string?, Guid> termsPredicate) where T : class, IDocumentModel
    {
        ManagedMetadata? metadata = null;

        var guid =
            termsPredicate.Invoke(model.DocumentInfo!.ContextUrl, columnName, value, null);

        if (guid != default)
        {
            metadata = new ManagedMetadata(guid, value.ToLower());
        }

        return metadata;
    }
}
