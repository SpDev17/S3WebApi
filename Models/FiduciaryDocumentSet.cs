using JetBrains.Annotations;
using S3WebApi.Types;
using System.Linq.Expressions;

namespace S3WebApi.Models;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class FiduciaryDocumentSet : DocumentSetBase
{
    public FiduciaryDocumentSet()
    {
    }

    public FiduciaryDocumentSet(IDocumentSetInfo documentSetInfo) : base(documentSetInfo)
    {
    }

    [ListItemColumn("CompanyNoLookup", Inherited = true)]
    public Lookup? CompanyNumber { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_Country", Inherited = true)]
    public Lookup? Country { get; set; }

    [ListItemColumn("DocumentSetDescription")]
    public string? DescriptionSet { get; set; }

    [ListItemColumn("ExtendedMetadata")]
    public string? ExtendedMetadata { get; set; }

    [ListItemColumn("Keywords", IsIncludedByDefault = true)]
    public string? Keywords { get; set; }

    [ListItemColumn("LegalHolds", IsTypeNotSupported = true)]
    public List<ManagedMetadata>? LegalHolds { get; set; }

    [ListItemColumn("MarshSystemIDLookup", IsTypeNotSupported = true)]
    public ManagedMetadata? MarshSystemId { get; set; }

    [ListItemColumn("MSO_x0020_Office", IsTypeNotSupported = true)]
    public ManagedMetadata? MsoOffice { get; set; }

    [ListItemColumn("TransactionNumber")]
    public string? TransactionNumber { get; set; }

    [ListItemColumn("Title", IsIncludedByDefault = true)]
    public string? Title { get; set; }

    public override bool IsDocumentSetValid(IConfiguration configuration)
    {
        var contentTypes =
            configuration
                .GetSection(DOCUMENT_SET_CONTENT_TYPE_SECTION)
                .GetSection(nameof(FiduciaryDocumentSet))
                .Get<List<string>>();

        return
            CompanyNumber != null &&
            Country != null &&
            ContentType != null &&
            contentTypes.Contains(ContentType);
    }

    public override Dictionary<string, object> Values<T>(
        Dictionary<string, string> columns,
        Expression<Func<T, object>>? fieldsToUpdate)
    {
        return ListItemBinder.GetValueSet(this, columns, fieldsToUpdate);
    }
}
