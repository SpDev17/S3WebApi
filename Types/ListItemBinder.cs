using System.Collections;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using PnP.Core.Model.SharePoint;
using S3WebApi.Models;
using S3WebApi.Models.Docs;
using Validation;
using ListItem = Microsoft.SharePoint.Client.ListItem;

namespace S3WebApi.Types;

public static class ListItemBinder
{
    private const string LookupFieldColumnFormat = "{0}LookupId";
    private const string LookupFieldColumnTypeFormat = "{0}LookupId@odata.type";
    private const string MultipleChoiceTypeFormat = "{0}@odata.type";
    private const string CollectionFieldType = "Collection(Edm.String)";
    private const string GuidSeparator = "-";
    private const string ManagedMetadataFormat = "-1;#{0}|{1}";
    private const string MultipleMetadataSeparator = ";#";
    private const string DlcDocumentIdLabel = "Description";
    private const string HyperLinkUrl = "Url";
    private const string ListColumnName = "Key";
    private const string ListColumnValue = "Value";
    private const string DateExceptionFormat = "MM/dd/yyyy hh:mm tt";
    private const string LookupIdField = "LookupId";
    private const string LookupValueField = "LookupValue";

    public static DataTable ToDataTable<T>(T model) where T : class, IListItemModel
    {
        Requires.NotNull(model, nameof(model));

        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dt = new DataTable();
        var row = dt.NewRow();

        foreach (var propertyInfo in properties)
        {
            var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

            if (ignore != null)
                continue;

            var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();
            var name = columnBinding?.Name ?? propertyInfo.Name;

            dt.Columns.Add(name);

            var value = propertyInfo.GetValue(model);

            if (value == null)
            {
                row[name] = value;

                continue;
            }

            var bindingType = GetBindingType(propertyInfo);

            switch (bindingType)
            {
                case ListItemBindingType.Undefined:
                    break;

                case ListItemBindingType.String:
                    row[name] = (string)value;
                    break;

                case ListItemBindingType.Integer:
                    row[name] = (int)value;
                    break;

                case ListItemBindingType.Double:
                    row[name] = (double)value;
                    break;

                case ListItemBindingType.Boolean:
                    row[name] = (bool)value;
                    break;

                case ListItemBindingType.DateTime:
                    row[name] = (DateTime)value;
                    break;

                case ListItemBindingType.Choice:
                    // No use case at the moment!
                    break;

                case ListItemBindingType.MultipleChoice:
                    // No use case at the moment!
                    break;

                case ListItemBindingType.Lookup:
                    row[name] = ((Lookup)value).Value;
                    break;

                case ListItemBindingType.MultipleLookup:
                    // No use case at the moment!
                    break;

                case ListItemBindingType.ManagedMetadata:
                    // No use case at the moment!
                    break;

                case ListItemBindingType.MultipleManageMetadata:
                    // No use case at the moment!
                    break;

                case ListItemBindingType.Hyperlink:
                    row[name] = ((Hyperlink)value).Value;
                    break;

                case ListItemBindingType.DocumentId:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        dt.Rows.Add(row);

        return dt;
    }

    public static T BindFromDataSetKeyValue<T>(DataSet data) where T : class, IListItemModel
    {
        Requires.NotNull(data, nameof(data));

        var instance = Activator.CreateInstance<T>();
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var map = new Dictionary<string, Tuple<ListItemBindingType, PropertyInfo>>();

        foreach (var propertyInfo in properties)
        {
            var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();
            var name = columnBinding?.Name ?? propertyInfo.Name;
            var bindingType = GetBindingType(propertyInfo);

            map.Add(name, new Tuple<ListItemBindingType, PropertyInfo>(bindingType, propertyInfo));
        }

        var table = data.Tables[0];

        foreach (DataRow row in table.Rows)
        {
            var listColumnName = (string)row[ListColumnName];
            var value = row[ListColumnValue];

            if (map.TryGetValue(listColumnName, out var info))
            {
                switch (info.Item1)
                {
                    case ListItemBindingType.Undefined:
                        break;
                    case ListItemBindingType.String:
                        info.Item2.SetValue(instance, Convert.ToString(value));
                        break;
                    case ListItemBindingType.Integer:
                        info.Item2.SetValue(instance, Convert.ToInt32(value));
                        break;
                    case ListItemBindingType.Double:
                        info.Item2.SetValue(instance, Convert.ToDouble(value));
                        break;
                    case ListItemBindingType.Boolean:
                        info.Item2.SetValue(instance, Convert.ToBoolean(value));
                        break;
                    case ListItemBindingType.DateTime:

                        if (DateTime.TryParseExact(Convert.ToString(value), DateExceptionFormat, null, DateTimeStyles.None, out var dt))
                        {
                            info.Item2.SetValue(instance, dt);
                        }

                        break;

                    case ListItemBindingType.Choice:
                    case ListItemBindingType.MultipleChoice:
                    case ListItemBindingType.Lookup:
                    case ListItemBindingType.MultipleLookup:
                    case ListItemBindingType.ManagedMetadata:
                    case ListItemBindingType.MultipleManageMetadata:
                    case ListItemBindingType.Hyperlink:
                    case ListItemBindingType.DocumentId:
                        throw new NotSupportedException("Not in use!");

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        return instance;
    }

    public static Dictionary<string, object> GetValueSet<T>(
        T model,
        Dictionary<string, string> columns,
        object? fieldsToUpdate) where T : class, IListItemEntity
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var values = new Dictionary<string, object>();

        List<string>? fields = null;

        if (fieldsToUpdate is Expression<Func<T, object>> selector)
            fields = GetSelectors(selector);

        foreach (var propertyInfo in properties)
        {
            if (fields != null && !fields.Contains(propertyInfo.Name))
                continue;

            var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

            if (ignore != null)
                continue;

            var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();

            var name = columnBinding?.Name ?? propertyInfo.Name;
            var readOnly = columnBinding?.ReadOnly ?? false;
            var isTypeNotSupported = columnBinding?.IsTypeNotSupported ?? false;
            var inherited = columnBinding?.Inherited ?? false;

            if (readOnly || isTypeNotSupported || inherited)
                continue;

            var value = propertyInfo.GetValue(model);

            if (value == null)
                continue;

            var bindingType = GetBindingType(propertyInfo);

            switch (bindingType)
            {
                case ListItemBindingType.Undefined:
                    break;

                case ListItemBindingType.String:
                    values.Add(name, (string)value);
                    break;

                case ListItemBindingType.Integer:
                    values.Add(name, (int)value);
                    break;

                case ListItemBindingType.Double:
                    values.Add(name, (double)value);
                    break;

                case ListItemBindingType.Boolean:
                    values.Add(name, (bool)value);
                    break;

                case ListItemBindingType.DateTime:

                    if ((DateTime)value == ListItemDefault.EmptyDateTime)
                        values.Add(name, null!);
                    else
                        values.Add(name, (DateTime)value);

                    break;

                case ListItemBindingType.Choice:
                    values.Add(name, ((Choice)value).Value);
                    break;

                case ListItemBindingType.MultipleChoice:

                    values.Add(string.Format(MultipleChoiceTypeFormat, name), CollectionFieldType);
                    values.Add(name, BuildMultipleChoice(value)!);

                    break;

                case ListItemBindingType.Lookup:

                    values.Add(
                        string.Format(LookupFieldColumnFormat, name),
                        ((Lookup)value).Value == ListItemDefault.EmptyLookup ? null! : ((Lookup)value).Value.ToString());

                    break;

                case ListItemBindingType.MultipleLookup:

                    values.Add(string.Format(LookupFieldColumnTypeFormat, name), CollectionFieldType);
                    values.Add(string.Format(LookupFieldColumnFormat, name), BuildMultipleLookup(value)!);

                    break;

                case ListItemBindingType.ManagedMetadata:

                    var (mmdId, mmdValue) = BuildManageMetadata(name, value, columns);

                    if (!string.IsNullOrWhiteSpace(mmdId))
                        values.Add(mmdId, mmdValue);

                    break;

                case ListItemBindingType.MultipleManageMetadata:

                    var (mmmdId, mmmdValue) = BuildMultipleManagedMetadata(name, value, columns);

                    if (!string.IsNullOrWhiteSpace(mmmdId))
                        values.Add(mmmdId, mmmdValue);

                    break;

                case ListItemBindingType.Hyperlink:
                    throw new InvalidOperationException();

                case ListItemBindingType.DocumentId:
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        return values;
    }

    public static void UpdateCsomListItem(
        ILogger logger,
        InstitutionLibrary library,
        IListItemEntity model,
        ClientContext context,
        FieldCollection fields,
        ListItem listItem)
    {
        switch (library)
        {
            case InstitutionLibrary.AccountManagement:
                UpdateCsomListItem(logger, (AccountManagementV1)model, context, fields, listItem);
                break;

            case InstitutionLibrary.Placements:
                UpdateCsomListItem(logger, (PlacementDocumentV1)model, context, fields, listItem);
                break;

            case InstitutionLibrary.Transactions:
                UpdateCsomListItem(logger, (TransactionDocumentV1)model, context, fields, listItem);
                break;

            case InstitutionLibrary.Policies:
                UpdateCsomListItem(logger, (PolicyDocumentV1)model, context, fields, listItem);
                break;

            case InstitutionLibrary.Fiduciary:
                UpdateCsomListItem(logger, (FiduciaryDocumentV1)model, context, fields, listItem);
                break;

            case InstitutionLibrary.Claims:
                UpdateCsomListItem(logger, (ClaimDocumentV1)model, context, fields, listItem);
                break;

            case InstitutionLibrary.Projects:
                UpdateCsomListItem(logger, (ProjectDocumentV1)model, context, fields, listItem);
                break;

            case InstitutionLibrary.Inbox:
                throw new InvalidOperationException();

            default:
                throw new ArgumentOutOfRangeException(nameof(library), library, null);
        }
    }

    private static void UpdateCsomListItem<T>(
        ILogger logger,
        T model,
        ClientRuntimeContext context,
        FieldCollection fields,
        ListItem listItem) where T : class, IListItemEntity
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var propertyInfo in properties)
        {
            var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

            if (ignore != null)
                continue;

            var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();

            var name = columnBinding?.Name ?? propertyInfo.Name;
            var readOnly = columnBinding?.ReadOnly ?? false;
            var inherited = columnBinding?.Inherited ?? false;

            if (readOnly || inherited)
                continue;

            var field = fields.GetFieldByInternalName(name);

            if (field == null)
            {
                //logger.Log().Warning("Missing field -> Name: {name}", name);

                continue;
            }

            var value = propertyInfo.GetValue(model);

            if (value == null)
                continue;

            var bindingType = GetBindingType(propertyInfo);

            switch (bindingType)
            {
                case ListItemBindingType.Undefined:
                    break;

                case ListItemBindingType.String:
                    listItem[name] = (string)value;
                    break;

                case ListItemBindingType.Integer:
                    listItem[name] = (int)value;
                    break;

                case ListItemBindingType.Double:
                    listItem[name] = (double)value;
                    break;

                case ListItemBindingType.Boolean:
                    listItem[name] = (bool)value;
                    break;

                case ListItemBindingType.DateTime:

                    var dateTimeValue = (DateTime)value;

                    if (dateTimeValue == DateTime.MinValue)
                        listItem[name] = null;
                    else
                        listItem[name] = dateTimeValue;

                    break;

                case ListItemBindingType.Choice:
                    throw new NotSupportedException("Column type is not supported!");

                case ListItemBindingType.MultipleChoice:
                    throw new NotSupportedException("Column type is not supported!");

                case ListItemBindingType.Lookup:

                    var lookupField = context.CastTo<FieldLookup>(fields.GetFieldByInternalName(name));
                    var lookupValue = new Microsoft.SharePoint.Client.FieldLookupValue { LookupId = ((Lookup)value).Value };

                    listItem[lookupField.InternalName] = lookupValue;

                    break;

                case ListItemBindingType.MultipleLookup:

                    var multipleLookupField = context.CastTo<FieldLookup>(fields.GetFieldByInternalName(name));
                    var lookupValues = new ArrayList();
                    var multipleLookup = (List<Lookup>)value;

                    foreach (var lookupEntry in multipleLookup.Select(lookupItem => new Microsoft.SharePoint.Client.FieldLookupValue { LookupId = lookupItem.Value }))
                    {
                        lookupValues.Add(lookupEntry);
                    }

                    listItem[multipleLookupField.InternalName] = lookupValues.ToArray();

                    break;

                case ListItemBindingType.ManagedMetadata:

                    var managedMetadataField = context.CastTo<TaxonomyField>(fields.GetFieldByInternalName(name));
                    var mmdValue = (ManagedMetadata)value;

                    if (mmdValue.TermGuid != Guid.Empty)
                    {
                        var taxonomyFieldValue = new TaxonomyFieldValue
                        {
                            Label = mmdValue.Label,
                            TermGuid = mmdValue.TermGuid.ToString()
                        };

                        managedMetadataField.SetFieldValueByValue(listItem, taxonomyFieldValue);
                    }

                    break;

                case ListItemBindingType.MultipleManageMetadata:

                    var multipleManagedMetadataField = context.CastTo<TaxonomyField>(fields.GetFieldByInternalName(name));
                    var mmmdValue = (List<ManagedMetadata>)value;
                    var sb = new StringBuilder();

                    foreach (var managedMetadata in mmmdValue.Where(managedMetadata => managedMetadata.TermGuid != Guid.Empty))
                    {
                        sb.Append($"-1;#{managedMetadata.Label}|{managedMetadata.TermGuid};#");
                    }

                    var termValues = sb.ToString().TrimEnd(";#".ToCharArray());

                    var taxonomyFieldValues =
                        new TaxonomyFieldValueCollection(
                            multipleManagedMetadataField.Context,
                            termValues,
                            multipleManagedMetadataField);

                    multipleManagedMetadataField.SetFieldValueByValueCollection(listItem, taxonomyFieldValues);

                    break;

                case ListItemBindingType.Hyperlink:
                    throw new NotSupportedException("Column type is not supported!");

                case ListItemBindingType.DocumentId:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public static IDocumentSetModel BindInstitutionFolder(
        InstitutionLibrary library,
        IDictionary<string, object> data,
        params object[] args)
    {
        IDocumentSetModel documentSet = library switch
        {
            InstitutionLibrary.AccountManagement => Bind<AccountManagementDocumentSet>(data, args),
            InstitutionLibrary.Placements => Bind<PlacementsDocumentSet>(data, args),
            InstitutionLibrary.Transactions => Bind<TransactionDocumentSet>(data, args),
            InstitutionLibrary.Policies => Bind<PolicyDocumentSet>(data, args),
            InstitutionLibrary.Fiduciary => Bind<FiduciaryDocumentSet>(data, args),
            InstitutionLibrary.Claims => Bind<ClaimDocumentSet>(data, args),
            InstitutionLibrary.Projects => Bind<ProjectDocumentSet>(data, args),
            InstitutionLibrary.Inbox => throw new InvalidOperationException(),
            _ => throw new ArgumentOutOfRangeException(nameof(library), library, null)
        };

        return documentSet;
    }

    public static IEnumerable<T> BindDriveItems<T>(IEnumerable<DriveItem> driveItems)
        where T : DocumentBaseV1
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var list = new List<T>();

        foreach (var driveItem in driveItems)
        {
            var data = driveItem.ListItem!.Fields!.AdditionalData;
            var instance = Activator.CreateInstance(typeof(T));

            foreach (var propertyInfo in properties)
            {
                var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

                if (ignore != null)
                    continue;

                var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();

                var name = columnBinding?.Name ?? propertyInfo.Name;
                var bindingType = GetBindingType(propertyInfo);

                if (bindingType is ListItemBindingType.Lookup or ListItemBindingType.MultipleLookup &&
                    !data.ContainsKey(name) &&
                    data.ContainsKey(string.Format(LookupFieldColumnFormat, name)))
                {
                    name = string.Format(LookupFieldColumnFormat, name);
                }

                if (!data.TryGetValue(name, out var value))
                    continue;

                switch (bindingType)
                {
                    case ListItemBindingType.Undefined:
                        break;

                    case ListItemBindingType.String:
                        propertyInfo.SetValue(instance, Convert.ToString(value));
                        break;

                    case ListItemBindingType.Integer:
                        propertyInfo.SetValue(instance, Convert.ToInt32(value));
                        break;

                    case ListItemBindingType.Double:
                        propertyInfo.SetValue(instance, Convert.ToDouble(value));
                        break;

                    case ListItemBindingType.Boolean:
                        propertyInfo.SetValue(instance, value);
                        break;

                    case ListItemBindingType.DateTime:
                        propertyInfo.SetValue(instance, value);
                        break;

                    case ListItemBindingType.Choice:
                        propertyInfo.SetValue(instance, new Choice(Convert.ToString(value)!));
                        break;

                    case ListItemBindingType.MultipleChoice:
                        propertyInfo.SetValue(instance, GetMultipleChoices(value));
                        break;

                    case ListItemBindingType.Lookup:
                        propertyInfo.SetValue(instance, new Lookup(Convert.ToInt32(value)));
                        break;

                    case ListItemBindingType.MultipleLookup:
                        propertyInfo.SetValue(instance, GetLookupList(value));
                        break;

                    case ListItemBindingType.ManagedMetadata:
                        propertyInfo.SetValue(instance, GetManagedMetadata(value));
                        break;

                    case ListItemBindingType.MultipleManageMetadata:
                        propertyInfo.SetValue(instance, GetManagedMetadataList(value));
                        break;

                    case ListItemBindingType.Hyperlink:
                        propertyInfo.SetValue(instance, GetHyperLink(value));
                        break;

                    case ListItemBindingType.DocumentId:
                        propertyInfo.SetValue(instance, GetDlcDocumentId(value));
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            // Process created and modified fields!

            if (driveItem.CreatedBy is { User: not null } && instance != null)
            {
                ((DocumentBaseV1)instance).CreatedBy =
                    new UserIdentityModel(
                        driveItem.CreatedBy.User.DisplayName,
                        driveItem.CreatedBy.User.AdditionalData);
            }

            if (driveItem.LastModifiedBy is { User: not null } && instance != null)
            {
                ((DocumentBaseV1)instance).LastModifiedBy =
                    new UserIdentityModel(
                        driveItem.LastModifiedBy.User.DisplayName,
                        driveItem.LastModifiedBy.User.AdditionalData);
            }

            list.Add((T)instance!);
        }

        return list;
    }

    public static IEnumerable<T> BindItems<T>(IEnumerable<IDictionary<string, object>> dataList)
        where T : class, IListItemEntity
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var list = new List<T>();

        foreach (var data in dataList)
        {
            var instance = Activator.CreateInstance(typeof(T));

            foreach (var propertyInfo in properties)
            {
                var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

                if (ignore != null)
                    continue;

                var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();

                var name = columnBinding?.Name ?? propertyInfo.Name;
                var bindingType = GetBindingType(propertyInfo);

                if (bindingType is ListItemBindingType.Lookup or ListItemBindingType.MultipleLookup &&
                    !data.ContainsKey(name) &&
                    data.ContainsKey(string.Format(LookupFieldColumnFormat, name)))
                {
                    name = string.Format(LookupFieldColumnFormat, name);
                }

                if (!data.TryGetValue(name, out var value))
                    continue;

                switch (bindingType)
                {
                    case ListItemBindingType.Undefined:
                        break;

                    case ListItemBindingType.String:
                        propertyInfo.SetValue(instance, Convert.ToString(value));
                        break;

                    case ListItemBindingType.Integer:
                        propertyInfo.SetValue(instance, Convert.ToInt32(value));
                        break;

                    case ListItemBindingType.Double:
                        propertyInfo.SetValue(instance, Convert.ToDouble(value));
                        break;

                    case ListItemBindingType.Boolean:
                        propertyInfo.SetValue(instance, value);
                        break;

                    case ListItemBindingType.DateTime:
                        propertyInfo.SetValue(instance, value);
                        break;

                    case ListItemBindingType.Choice:
                        propertyInfo.SetValue(instance, new Choice(Convert.ToString(value)!));
                        break;

                    case ListItemBindingType.MultipleChoice:
                        propertyInfo.SetValue(instance, GetMultipleChoices(value));
                        break;

                    case ListItemBindingType.Lookup:
                        propertyInfo.SetValue(instance, new Lookup(Convert.ToInt32(value)));
                        break;

                    case ListItemBindingType.MultipleLookup:
                        propertyInfo.SetValue(instance, GetLookupList(value));
                        break;

                    case ListItemBindingType.ManagedMetadata:
                        propertyInfo.SetValue(instance, GetManagedMetadata(value));
                        break;

                    case ListItemBindingType.MultipleManageMetadata:
                        propertyInfo.SetValue(instance, GetManagedMetadataList(value));
                        break;

                    case ListItemBindingType.Hyperlink:
                        propertyInfo.SetValue(instance, GetHyperLink(value));
                        break;

                    case ListItemBindingType.DocumentId:
                        propertyInfo.SetValue(instance, GetDlcDocumentId(value));
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            list.Add((T)instance!);
        }

        return list;
    }

    public static IEnumerable<T> BindList<T>(IEnumerable<IDictionary<string, object>> listData)
        where T : class, IListItemEntity
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var list = new List<T>();

        foreach (var data in listData)
        {
            var instance = Activator.CreateInstance(typeof(T));

            foreach (var propertyInfo in properties)
            {
                var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

                if (ignore != null)
                {
                    continue;
                }

                var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();

                var name = columnBinding?.Name ?? propertyInfo.Name;
                var bindingType = GetBindingType(propertyInfo);

                if (bindingType is ListItemBindingType.Lookup or ListItemBindingType.MultipleLookup &&
                    !data.ContainsKey(name) &&
                    data.ContainsKey(string.Format(LookupFieldColumnFormat, name)))
                {
                    name = string.Format(LookupFieldColumnFormat, name);
                }

                if (!data.TryGetValue(name, out var value))
                {
                    continue;
                }

                switch (bindingType)
                {
                    case ListItemBindingType.Undefined:
                        break;

                    case ListItemBindingType.String:
                        propertyInfo.SetValue(instance, Convert.ToString(value));
                        break;

                    case ListItemBindingType.Integer:
                        propertyInfo.SetValue(instance, Convert.ToInt32(value));
                        break;

                    case ListItemBindingType.Double:
                        propertyInfo.SetValue(instance, Convert.ToDouble(value));
                        break;

                    case ListItemBindingType.Boolean:
                        propertyInfo.SetValue(instance, value);
                        break;

                    case ListItemBindingType.DateTime:
                        propertyInfo.SetValue(instance, value);
                        break;

                    case ListItemBindingType.Choice:
                        propertyInfo.SetValue(instance, new Choice(Convert.ToString(value)!));
                        break;

                    case ListItemBindingType.MultipleChoice:
                        propertyInfo.SetValue(instance, GetMultipleChoices(value));
                        break;

                    case ListItemBindingType.Lookup:
                        propertyInfo.SetValue(instance, new Lookup(Convert.ToInt32(value)));
                        break;

                    case ListItemBindingType.MultipleLookup:
                        propertyInfo.SetValue(instance, GetLookupList(value));
                        break;

                    case ListItemBindingType.ManagedMetadata:
                        propertyInfo.SetValue(instance, GetManagedMetadata(value));
                        break;

                    case ListItemBindingType.MultipleManageMetadata:
                        propertyInfo.SetValue(instance, GetManagedMetadataList(value));
                        break;

                    case ListItemBindingType.Hyperlink:
                        propertyInfo.SetValue(instance, GetHyperLink(value));
                        break;

                    case ListItemBindingType.DocumentId:
                        propertyInfo.SetValue(instance, GetDlcDocumentId(value));
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            list.Add((T)instance!);
        }

        return list;
    }

    public static T Bind<T>(IDictionary<string, object> data, params object[] args) where T : class, IListItemEntity
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var instance = Activator.CreateInstance(typeof(T), args);

        foreach (var propertyInfo in properties)
        {
            var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

            if (ignore != null)
                continue;

            var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();

            var name = columnBinding?.Name ?? propertyInfo.Name;
            var bindingType = GetBindingType(propertyInfo);

            if (bindingType is ListItemBindingType.Lookup or ListItemBindingType.MultipleLookup &&
                !data.ContainsKey(name) &&
                data.ContainsKey(string.Format(LookupFieldColumnFormat, name)))
            {
                name = string.Format(LookupFieldColumnFormat, name);
            }

            if (!data.TryGetValue(name, out var value))
                continue;

            switch (bindingType)
            {
                case ListItemBindingType.Undefined:
                    break;

                case ListItemBindingType.String:
                    propertyInfo.SetValue(instance, Convert.ToString(value));
                    break;

                case ListItemBindingType.Integer:
                    propertyInfo.SetValue(instance, Convert.ToInt32(value));
                    break;

                case ListItemBindingType.Double:
                    propertyInfo.SetValue(instance, Convert.ToDouble(value));
                    break;

                case ListItemBindingType.Boolean:
                    propertyInfo.SetValue(instance, value);
                    break;

                case ListItemBindingType.DateTime:
                    propertyInfo.SetValue(instance, value);
                    break;

                case ListItemBindingType.Choice:
                    propertyInfo.SetValue(instance, new Choice(Convert.ToString(value)!));
                    break;

                case ListItemBindingType.MultipleChoice:
                    propertyInfo.SetValue(instance, GetMultipleChoices(value));
                    break;

                case ListItemBindingType.Lookup:
                    propertyInfo.SetValue(instance, new Lookup(Convert.ToInt32(value)));
                    break;

                case ListItemBindingType.MultipleLookup:
                    propertyInfo.SetValue(instance, GetLookupList(value));
                    break;

                case ListItemBindingType.ManagedMetadata:
                    propertyInfo.SetValue(instance, GetManagedMetadata(value));
                    break;

                case ListItemBindingType.MultipleManageMetadata:
                    propertyInfo.SetValue(instance, GetManagedMetadataList(value));
                    break;

                case ListItemBindingType.Hyperlink:
                    propertyInfo.SetValue(instance, GetHyperLink(value));
                    break;

                case ListItemBindingType.DocumentId:
                    propertyInfo.SetValue(instance, GetDlcDocumentId(value));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return (T)instance!;
    }

    public static bool HasUnUnsupportedTypes<T>(
        T model,
        Expression<Func<T, object>>? fieldsToUpdate) where T : class, IListItemEntity
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var fields = GetSelectors(fieldsToUpdate);

        if (fields != null)
            return properties
                .Where(propertyInfo => fields.Contains(propertyInfo.Name))
                .Select(propertyInfo => propertyInfo.GetCustomAttribute<ListItemColumnAttribute>())
                .Select(columnBinding => columnBinding?.IsTypeNotSupported ?? false)
                .Any(isTypeNotSupported => isTypeNotSupported);

        return properties
            .Select(propertyInfo => propertyInfo.GetCustomAttribute<ListItemColumnAttribute>())
            .Select(columnBinding => columnBinding?.IsTypeNotSupported ?? false)
            .Any(isTypeNotSupported => isTypeNotSupported);
    }

    public static bool UpdateUnsupportedTypes<T>(
        T model,
        IListItem listItem,
        Expression<Func<T, object>>? fieldsToUpdate) where T : class, IListItemEntity
    {
        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var fields = GetSelectors(fieldsToUpdate);
        var hasModifiedProperty = false;

        foreach (var propertyInfo in properties)
        {
            if (fields != null && !fields.Contains(propertyInfo.Name))
                continue;

            var ignore = propertyInfo.GetCustomAttribute<IgnoreBindingAttribute>();

            if (ignore != null)
                continue;

            var columnBinding = propertyInfo.GetCustomAttribute<ListItemColumnAttribute>();

            var name = columnBinding?.Name ?? propertyInfo.Name;
            var isTypeNotSupported = columnBinding?.IsTypeNotSupported ?? false;

            if (!isTypeNotSupported)
                continue;

            var value = propertyInfo.GetValue(model);

            if (value == null)
                continue;

            var bindingType = GetBindingType(propertyInfo);

            switch (bindingType)
            {
                case ListItemBindingType.ManagedMetadata:

                    var mmdValue = (ManagedMetadata)value;

                    if (mmdValue.TermGuid == Guid.Empty && mmdValue.Label == string.Empty)
                        listItem[name] = null;
                    else
                        listItem[name] = new FieldTaxonomyValue(mmdValue.TermGuid, mmdValue.Label);

                    hasModifiedProperty = true;

                    break;

                case ListItemBindingType.MultipleManageMetadata:

                    var mmmdValue = (List<ManagedMetadata>)value;

                    if (!mmmdValue.Any())
                        listItem[name] = new FieldValueCollection();
                    else
                    {
                        var taxonomyCollection = new FieldValueCollection();

                        foreach (var item in mmmdValue)
                        {
                            taxonomyCollection.Values.Add(
                                new FieldTaxonomyValue(item.TermGuid, item.Label));
                        }

                        listItem[name] = taxonomyCollection;
                    }

                    hasModifiedProperty = true;

                    break;

                // Supported fields!
                case ListItemBindingType.Undefined:
                case ListItemBindingType.String:
                case ListItemBindingType.Integer:
                case ListItemBindingType.Double:
                case ListItemBindingType.Boolean:
                case ListItemBindingType.DateTime:
                case ListItemBindingType.Choice:
                case ListItemBindingType.MultipleChoice:
                case ListItemBindingType.Lookup:
                case ListItemBindingType.MultipleLookup:
                case ListItemBindingType.Hyperlink:
                case ListItemBindingType.DocumentId:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return hasModifiedProperty;
    }

    private static List<string>? GetSelectors<T>(Expression<Func<T, object>>? fieldsToUpdate)
        where T : class, IListItemEntity
    {
        if (fieldsToUpdate is not { Body: NewExpression ne })
            return null;

        return (from me in ne.Arguments.Select(arg => arg as MemberExpression)
                where me != null
                select GetFieldName(me))
            .ToList();
    }

    private static string GetFieldName(Expression e)
    {
        var me = e as MemberExpression;
        var ue = e as UnaryExpression;

        if (me == null && ue?.Operand is not MemberExpression)
            throw new NotSupportedException();

        var member = me != null ? me.Member : ((MemberExpression)ue!.Operand).Member;

        return member.Name;
    }

    private static string[]? BuildMultipleChoice(object value)
    {
        if (value is not List<Choice> list)
            throw new InvalidOperationException("Invalid multiple choice column binding!");

        // Empty indicator, this will be translated as 'clear' the column value!
        if (list.Count == 1 && list.First().Value == string.Empty)
            return null;

        return list.Select(s => s.Value).ToArray();
    }

    private static string[]? BuildMultipleLookup(object value)
    {
        if (value is not List<Lookup> list)
            throw new InvalidOperationException("Invalid multiple lookup column binding!");

        // Empty indicator, this will be translated as 'clear' the column value!
        if (list.Count == 1 && list.First().Value == ListItemDefault.EmptyLookup)
            return null;

        return list.Select(s => s.Value.ToString()).ToArray();
    }

    private static Tuple<string, string> BuildManageMetadata(
        string name,
        object value,
        IReadOnlyDictionary<string, string> columns)
    {
        if (value is not ManagedMetadata mmd)
            throw new InvalidOperationException("Invalid managed metadata column binding!");

        if (!columns.TryGetValue(name, out var mmdColumnId))
            return new Tuple<string, string>(string.Empty, string.Empty);

        var id = mmdColumnId.Replace(GuidSeparator, string.Empty);
        var termId = mmd.TermGuid.ToString().Replace(GuidSeparator, string.Empty);

        // If both are empty, this will be translated as 'clear' the column value!
        if (mmd.TermGuid == Guid.Empty && mmd.Label == string.Empty)
            return new Tuple<string, string>(id, string.Empty);

        return new Tuple<string, string>(id, string.Format(ManagedMetadataFormat, mmd.Label, termId));
    }

    private static Tuple<string, string> BuildMultipleManagedMetadata(
        string name,
        object value,
        IReadOnlyDictionary<string, string> columns)
    {
        if (value is not List<ManagedMetadata> mmmd)
            throw new InvalidOperationException("Invalid multiple managed metadata column binding!");

        if (!columns.TryGetValue(name, out var mmmdColumnId))
            return new Tuple<string, string>(string.Empty, string.Empty);

        var id = mmmdColumnId.Replace(GuidSeparator, string.Empty);
        var sb = new StringBuilder();

        // If the count is exactly 1, and both id and labels are empty,
        // translate it as 'clear' the column value!
        if (mmmd.Count == 1 && mmmd.First().TermGuid == Guid.Empty && mmmd.First().Label == string.Empty)
            return new Tuple<string, string>(id, string.Empty);

        foreach (var managedMetadata in mmmd)
        {
            var termId = managedMetadata.TermGuid.ToString().Replace(GuidSeparator, string.Empty);

            sb.Append(string.Format(ManagedMetadataFormat, managedMetadata.Label, termId));
            sb.Append(MultipleMetadataSeparator);
        }

        var multipleMetadata = sb.ToString().TrimEnd(MultipleMetadataSeparator.ToCharArray());

        return new Tuple<string, string>(id, multipleMetadata);
    }

    private static List<Choice>? GetMultipleChoices(object value)
    {
        if (value is not UntypedArray uaChoices)
            return null;

        var choices = new List<string>();

        foreach (var item in uaChoices.GetValue())
        {
            if (item is not UntypedString us)
                continue;

            var v = us.GetValue();

            if (!string.IsNullOrWhiteSpace(v))
                choices.Add(v);
        }

        return choices.Select(s => new Choice(s)).ToList();
    }

    private static List<Lookup>? GetLookupList(object value)
    {
        if (value is not UntypedArray uaLookups)
            return null;

        var lookupData = new List<Tuple<int, string?>>();

        foreach (var item in uaLookups.GetValue())
        {
            if (item is not UntypedObject uo)
                continue;

            var values = uo.GetValue();
            var lookupId = values[LookupIdField];
            var lookupValue = values[LookupValueField];

            if (lookupId is UntypedInteger id && lookupValue is UntypedString description)
            {
                lookupData.Add(new Tuple<int, string?>(id.GetValue(), description.GetValue()));
            }
        }

        return lookupData.Select(s => Lookup.Factory(s.Item1, s.Item2)).ToList();
    }

    private static List<ManagedMetadata>? GetManagedMetadataList(object value)
    {
        if (value is not UntypedArray uoMetadataList)
            return null;

        var list = new List<ManagedMetadata>();

        foreach (var mmd in uoMetadataList.GetValue())
        {
            if (mmd is not UntypedObject uoMetadata)
                continue;

            var metadata = GetManagedMetadata(uoMetadata);

            if (metadata != null)
                list.Add(metadata);
        }

        return list;
    }

    private static ManagedMetadata? GetManagedMetadata(object? value)
    {
        if (value is not UntypedObject uoMetadata)
            return null;

        var label = string.Empty;
        var termGuid = string.Empty;

        foreach (var mmd in uoMetadata.GetValue())
        {
            switch (mmd.Key)
            {
                case nameof(ManagedMetadata.Label):
                    {
                        if (mmd.Value is UntypedString mmdValue)
                            label = mmdValue.GetValue();

                        break;
                    }
                case nameof(ManagedMetadata.TermGuid):
                    {
                        if (mmd.Value is UntypedString mmdValue)
                            termGuid = mmdValue.GetValue();

                        break;
                    }
            }
        }

        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(termGuid))
            return null;

        return new ManagedMetadata(Guid.Parse(termGuid), label);
    }

    private static Hyperlink? GetHyperLink(object? value)
    {
        if (value is not UntypedObject uoHyperlink)
            return null;

        foreach (var dlc in uoHyperlink.GetValue())
        {
            if (dlc is not { Key: HyperLinkUrl, Value: UntypedString dlcValue })
                continue;

            var v = dlcValue.GetValue();

            if (v != null)
                return new Hyperlink(v);
        }

        return null;
    }

    private static DocumentId? GetDlcDocumentId(object? value)
    {
        if (value is not UntypedObject uoDocumentId)
            return null;

        foreach (var dlc in uoDocumentId.GetValue())
        {
            if (dlc is not { Key: DlcDocumentIdLabel, Value: UntypedString dlcValue })
                continue;

            var v = dlcValue.GetValue();

            if (v != null)
                return new DocumentId(v);
        }

        return null;
    }

    public static ListItemBindingType GetBindingType(PropertyInfo pi)
    {
        var type = pi.PropertyType;

        return
            type == typeof(string) ? ListItemBindingType.String :
            type == typeof(int) || type == typeof(int?) ? ListItemBindingType.Integer :
            type == typeof(double) || type == typeof(double?) ? ListItemBindingType.Double :
            type == typeof(bool) || type == typeof(bool?) ? ListItemBindingType.Boolean :
            type == typeof(DateTime) || type == typeof(DateTime?) ? ListItemBindingType.DateTime :
            type == typeof(Choice) ? ListItemBindingType.Choice :
            type == typeof(List<Choice>) ? ListItemBindingType.MultipleChoice :
            type == typeof(Lookup) ? ListItemBindingType.Lookup :
            type == typeof(List<Lookup>) ? ListItemBindingType.MultipleLookup :
            type == typeof(ManagedMetadata) ? ListItemBindingType.ManagedMetadata :
            type == typeof(List<ManagedMetadata>) ? ListItemBindingType.MultipleManageMetadata :
            type == typeof(Hyperlink) ? ListItemBindingType.Hyperlink :
            type == typeof(DocumentId) ? ListItemBindingType.DocumentId :
            ListItemBindingType.Undefined;
    }
}
