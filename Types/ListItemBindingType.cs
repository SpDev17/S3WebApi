namespace S3WebApi.Types;

public enum ListItemBindingType
{
    Undefined,
    String,
    Integer,
    Double,
    Boolean,
    DateTime,
    Choice,
    MultipleChoice,
    Lookup,
    MultipleLookup,
    ManagedMetadata,
    MultipleManageMetadata,
    Hyperlink,
    DocumentId
}
