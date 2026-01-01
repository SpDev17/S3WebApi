namespace S3WebApi.Types;

public static class ListItemDefault
{
    public static readonly int EmptyLookup = 0;
    public static readonly DateTime EmptyDateTime = DateTime.MinValue;
    public static readonly Choice EmptyChoice = new(string.Empty);
    public static readonly List<Choice> EmptyMultipleChoice = new() { EmptyChoice };
    public static readonly Lookup EmptyLookupId = new(0);
    public static readonly List<Lookup> EmptyMultipleLookupId = new() { EmptyLookupId };
    public static readonly ManagedMetadata EmptyManagedMetadata = ManagedMetadata.Empty();
    public static readonly List<ManagedMetadata> EmptyMultipleManagedMetadata = new() { EmptyManagedMetadata };
}
