using JetBrains.Annotations;
using S3WebApi.Types;
using System.Linq.Expressions;

namespace S3WebApi.Models;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class PolicyDocumentSet : DocumentSetBase
{
    public PolicyDocumentSet()
    {
    }

    public PolicyDocumentSet(IDocumentSetInfo documentSetInfo) : base(documentSetInfo)
    {
    }

    [ListItemColumn("BillingIDLookup")]
    public List<Lookup>? BillingIds { get; set; }

    [ListItemColumn("CarrierIDLookup")]
    public List<Lookup>? CarrierIds { get; set; }

    [ListItemColumn("CarrierNameLookup")]
    public List<Lookup>? CarrierNames { get; set; }

    [ListItemColumn("CarrierPolicyNoLookup")]
    public List<Lookup>? CarrierPolicyNumbers { get; set; }

    [ListItemColumn("Carrier_x0020_PolicyNo")]
    public string? CarrierPolicyNumberString { get; set; }

    [ListItemColumn("CarrierID")]
    public string? CarrierId { get; set; }

    [ListItemColumn("CarrierName1")]
    public string? CarrierName { get; set; }

    [ListItemColumn("CompanyNoLookup", Inherited = true)]
    public Lookup? CompanyNumber { get; set; }

    [ListItemColumn("ContractDescription")]
    public string? ContractDescription { get; set; }

    [ListItemColumn("ContractOfficeLookup", IsTypeNotSupported = true)]
    public ManagedMetadata? ContractOffice { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_Country", Inherited = true)]
    public Lookup? Country { get; set; }

    [ListItemColumn("CoverageTypeLookup", IsTypeNotSupported = true)]
    public List<ManagedMetadata>? Coverages { get; set; }

    [ListItemColumn("DocumentSetDescription")]
    public string? DescriptionSet { get; set; }

    [ListItemColumn("ExtendedMetadata")]
    public string? ExtendedMetadata { get; set; }

    [ListItemColumn("FirstNamedInsured")]
    public string? FirstNamedInsured { get; set; }

    [ListItemColumn("Keywords", IsIncludedByDefault = true)]
    public string? Keywords { get; set; }

    [ListItemColumn("LegalHolds", IsTypeNotSupported = true)]
    public List<ManagedMetadata>? LegalHolds { get; set; }

    [ListItemColumn("MarshSystemIDLookup", IsTypeNotSupported = true)]
    public ManagedMetadata? MarshSystemId { get; set; }

    [ListItemColumn("MSO_x0020_Office", IsTypeNotSupported = true)]
    public ManagedMetadata? MsoOffice { get; set; }

    [ListItemColumn("PolicyEffectiveDateLookup")]
    public List<Lookup>? PolicyEffectiveDates { get; set; }

    // ReSharper disable once StringLiteralTypo
    [ListItemColumn("Policy_x0020_Effectivedate", IsIncludedByDefault = true)]
    public DateTime? PolicyEffectiveDate { get; set; }

    [ListItemColumn("PolicyExpirationDate", IsIncludedByDefault = true)]
    public DateTime? PolicyExpirationDate { get; set; }

    [ListItemColumn("ContractID")]
    public string? PolicyHeader { get; set; }

    [ListItemColumn("ContractIDLookup")]
    public List<Lookup>? PolicyHeaders { get; set; }

    [ListItemColumn("ThirdPartyID")]
    public string? ThirdPartyId { get; set; }

    [ListItemColumn("Title", IsIncludedByDefault = true)]
    public string? Title { get; set; }

    public override bool IsDocumentSetValid(IConfiguration configuration)
    {
        var contentTypes =
            configuration
                .GetSection(DOCUMENT_SET_CONTENT_TYPE_SECTION)
                .GetSection(nameof(PolicyDocumentSet))
                .Get<List<string>>();

        var isPolicyRuleValid =
            !string.IsNullOrWhiteSpace(PolicyHeader) || (CarrierPolicyNumbers != null && CarrierPolicyNumbers.Any());

        return
            CompanyNumber != null &&
            Country != null &&
            Coverages != null &&
            Coverages.Any() &&
            PolicyEffectiveDates != null &&
            PolicyEffectiveDates.Any() &&
            isPolicyRuleValid &&
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