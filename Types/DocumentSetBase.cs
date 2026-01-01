using JetBrains.Annotations;
using System.Linq.Expressions;
using Validation;

namespace S3WebApi.Types;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public abstract class DocumentSetBase : IDocumentSetModel
{
    protected const string DOCUMENT_SET_CONTENT_TYPE_SECTION = "DocumentSetContentTypes";

    protected DocumentSetBase()
    {
    }

    protected DocumentSetBase(IDocumentSetInfo documentSetInfo)
    {
        Requires.NotNull(documentSetInfo, nameof(documentSetInfo));

        DocumentSetInfo = documentSetInfo;
    }

    [ListItemColumn("ContentType", ReadOnly = true, IsIncludedByDefault = true)]
    public string? ContentType { get; set; }

    [ListItemColumn("FileLeafRef", ReadOnly = true, IsIncludedByDefault = true)]
    public string? FileLeafRef { get; set; }

    [ListItemColumn("ObjectID", IsIncludedByDefault = true)]
    public string? ObjectId { get; set; }

    [ListItemColumn("Created", ReadOnly = true, IsIncludedByDefault = true)]
    public DateTime? CreatedDateUtc { get; set; }

    [ListItemColumn("Modified", ReadOnly = true, IsIncludedByDefault = true)]
    public DateTime? ModifiedDateUtc { get; set; }

    [IgnoreBinding]
    public IDocumentSetInfo? DocumentSetInfo { get; }

    public abstract bool IsDocumentSetValid(IConfiguration configuration);

    public abstract Dictionary<string, object> Values<T>(
        Dictionary<string, string> columns,
        Expression<Func<T, object>>? fieldsToUpdate) where T : class, IListItemEntity;
}