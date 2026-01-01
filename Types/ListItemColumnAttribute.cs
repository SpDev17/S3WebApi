namespace S3WebApi.Types;

[AttributeUsage(AttributeTargets.Property)]
public class ListItemColumnAttribute : Attribute
{
    public ListItemColumnAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
    public bool ReadOnly { get; set; }
    public bool IsTypeNotSupported { get; set; }
    public bool Inherited { get; set; }

    /// <summary>
    /// Hide field from client mapping.
    /// </summary>
    public bool ExcludeInClientFieldMapping { get; set; }

    /// <summary>
    /// Included in the select clause field query by default.
    /// </summary>
    public bool IsIncludedByDefault { get; set; }
}

