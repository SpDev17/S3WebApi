using JetBrains.Annotations;
using S3WebApi.Types;
using System.Linq.Expressions;

namespace S3WebApi.Models;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PlacementsDocumentSet : DocumentSetBase
{
    public PlacementsDocumentSet()
    {
    }

    public PlacementsDocumentSet(IDocumentSetInfo documentSetInfo) : base(documentSetInfo)
    {
    }

    [ListItemColumn("CompanyNoLookup", Inherited = true)]
    public Lookup? CompanyNumber { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_Country", Inherited = true)]
    public Lookup? Country { get; set; }

    [ListItemColumn("CoverageTypeLookup", IsTypeNotSupported = true)]
    public List<ManagedMetadata>? Coverages { get; set; }

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

    [ListItemColumn("SubmissionHeader", IsIncludedByDefault = true)]
    public string? PlacementHeader { get; set; }

    [ListItemColumn("PlacementHubLookup", IsTypeNotSupported = true)]
    public ManagedMetadata? PlacementHub { get; set; }

    [ListItemColumn("ContractIDLookup")]
    public List<Lookup>? PolicyHeader { get; set; }

    [ListItemColumn("Reinsurance")]
    public bool? Reinsurance { get; set; }

    [ListItemColumn("SubmissionEffectiveDate")]
    public DateTime? SubmissionEffectiveDate { get; set; }

    [ListItemColumn("SubmissionExpirationDate")]
    public DateTime? SubmissionExpirationDate { get; set; }

    [ListItemColumn("SubmissionID", IsIncludedByDefault = true)]
    public string? SubmissionId { get; set; }

    [ListItemColumn("Title", IsIncludedByDefault = true)]
    public string? Title { get; set; }

    public override bool IsDocumentSetValid(IConfiguration configuration)
    {
        var contentTypes =
            configuration
                .GetSection(DOCUMENT_SET_CONTENT_TYPE_SECTION)
                .GetSection(nameof(PlacementsDocumentSet))
                .Get<List<string>>();

        return
            CompanyNumber != null &&
            Country != null &&
            !string.IsNullOrWhiteSpace(PlacementHeader) &&
            Coverages != null &&
            Coverages.Any() &&
            SubmissionEffectiveDate != null &&
            SubmissionExpirationDate != null &&
            !string.IsNullOrWhiteSpace(SubmissionId) &&
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