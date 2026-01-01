namespace S3WebApi.Types;

public class ManagedMetadata
{
    public ManagedMetadata(Guid termId, string label)
    {
        TermGuid = termId;
        Label = label;
    }

    public Guid TermGuid { get; }
    public string Label { get; }

    public static ManagedMetadata Empty()
    {
        return new ManagedMetadata(Guid.Empty, string.Empty);
    }
}