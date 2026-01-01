using JetBrains.Annotations;
using S3WebApi.Types;
using System.Linq.Expressions;

namespace S3WebApi.Models;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ClaimDocumentSet : DocumentSetBase
{
    public ClaimDocumentSet()
    {
    }

    public ClaimDocumentSet(IDocumentSetInfo documentSetInfo) : base(documentSetInfo)
    {
    }

    [ListItemColumn("CarrierClaimNoLookup")]
    public List<Lookup>? CarrierClaimNumber { get; set; }

    [ListItemColumn("CarrierPolicyNoLookup")]
    public List<Lookup>? CarrierPolicyNumber { get; set; }

    [ListItemColumn("ClaimName", IsIncludedByDefault = true)]
    public string? ClaimName { get; set; }

    [ListItemColumn("ClaimTypeLookup", IsTypeNotSupported = true)]
    public ManagedMetadata? ClaimType { get; set; }

    [ListItemColumn("CompanyNoLookup", Inherited = true)]
    public Lookup? CompanyNumber { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_Country", Inherited = true)]
    public Lookup? Country { get; set; }

    [ListItemColumn("CoverageTypeLookup", IsTypeNotSupported = true)]
    public List<ManagedMetadata>? Coverages { get; set; }

    [ListItemColumn("DateOfClaimClosed")]
    public DateTime? DateOfClaimClosed { get; set; }

    [ListItemColumn("DocumentSetDescription")]
    public string? DescriptionSet { get; set; }

    [ListItemColumn("ExtendedMetadata")]
    public string? ExtendedMetadata { get; set; }

    [ListItemColumn("Keywords", IsIncludedByDefault = true)]
    public string? Keywords { get; set; }

    [ListItemColumn("LegalHolds", IsTypeNotSupported = true)]
    public List<ManagedMetadata>? LegalHolds { get; set; }

    [ListItemColumn("LossDate")]
    public DateTime? LossDate { get; set; }

    [ListItemColumn("MarshClaimNo", IsIncludedByDefault = true)]
    public string? MarshClaimNumber { get; set; }

    [ListItemColumn("MarshSystemIDLookup", IsTypeNotSupported = true)]
    public ManagedMetadata? MarshSystemId { get; set; }

    [ListItemColumn("MSO_x0020_Office", IsTypeNotSupported = true)]
    public ManagedMetadata? MsoOffice { get; set; }

    [ListItemColumn("ContractIDLookup")]
    public List<Lookup>? PolicyHeader { get; set; }

    [ListItemColumn("RecoveryNo")]
    public string? RecoveryNo { get; set; }

    [ListItemColumn("Title", IsIncludedByDefault = true)]
    public string? Title { get; set; }

    public override bool IsDocumentSetValid(IConfiguration configuration)
    {
        var contentTypes =
            configuration
                .GetSection(DOCUMENT_SET_CONTENT_TYPE_SECTION)
                .GetSection(nameof(ClaimDocumentSet))
                .Get<List<string>>();

        return
            CompanyNumber != null &&
            Country != null &&
            !string.IsNullOrWhiteSpace(MarshClaimNumber) &&
            !string.IsNullOrWhiteSpace(ClaimName) &&
            LossDate != null &&
            Coverages != null &&
            Coverages.Any() &&
            ClaimType != null &&
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