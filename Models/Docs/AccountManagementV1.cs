using JetBrains.Annotations;
using S3WebApi.Types;
using System.Linq.Expressions;

namespace S3WebApi.Models.Docs;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class AccountManagementV1 : DocumentBaseV1
{
    public AccountManagementV1()
    {
    }

    public AccountManagementV1(IDocumentInfo documentInfo) : base(documentInfo)
    {
    }

    // Excluded: CheckoutUser, CreatedBy, ModifiedBy

    [ListItemColumn("CompanyNoLookup_x003A_MarshAccountName", Inherited = true)]
    public Lookup? AccountName { get; set; }

    [ListItemColumn("AliasFlag")]
    public string? AliasFlag { get; set; }

    [ListItemColumn("AttachmentIndicator")]
    public double? AttachmentIndicator { get; set; }

    [ListItemColumn("BatchID")]
    public string? BatchId { get; set; }

    [ListItemColumn("BCC")]
    public string? Bcc { get; set; }

    [ListItemColumn("CC")]
    public string? Cc { get; set; }

    [ListItemColumn("ChildDocIds")]
    public string? ChildDocIds { get; set; }

    [ListItemColumn("CompanyNoLookup", Inherited = true)]
    public Lookup? CompanyNumber { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_Country", Inherited = true)]
    public Lookup? Country { get; set; }

    [ListItemColumn("Description")]
    public string? Description { get; set; }

    [ListItemColumn("_ExtendedDescription")]
    public string? DescriptionExtended { get; set; }

    [ListItemColumn("DocumentSetDescription", Inherited = true)]
    public string? DescriptionSet { get; set; }

    [ListItemColumn("DocumentDate")]
    public DateTime? DocumentDate { get; set; }

    [ListItemColumn("DocumentTypeLookup", IsTypeNotSupported = true)]
    public ManagedMetadata? DocumentType { get; set; }

    [ListItemColumn("EffectiveDate")]
    public DateTime? EffectiveDate { get; set; }

    [ListItemColumn("ExpirationDate")]
    public DateTime? ExpirationDate { get; set; }

    [ListItemColumn("ExtendedMetadata")]
    public string? ExtendedMetadata { get; set; }

    [ListItemColumn("ExternalSourceDocID")]
    public string? ExternalSourceDocId { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_FamilyCode", Inherited = true)]
    public Lookup? FamilyCode { get; set; }

    [ListItemColumn("FROM_0ed5db1a_x002d_1788_x002d_4a9e_x002d_9900_x002d_d991d3dbe78d")]
    public string? From { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_GUAccountName", Inherited = true)]
    public Lookup? GuAccountName { get; set; }

    // ReSharper disable once StringLiteralTypo
    [ListItemColumn("CompanyNoLookup_x003A_GUDUNSNumber", Inherited = true)]
    public Lookup? GuDunsNumber { get; set; }

    [ListItemColumn("HiddenFlg")]
    public bool? HiddenFlag { get; set; }

    [ListItemColumn("KeyValueAttribute")]
    public string? KeyValueAttribute { get; set; }

    [ListItemColumn("Keywords", IsIncludedByDefault = true)]
    public string? Keywords { get; set; }

    [ListItemColumn("LegacyDocID")]
    public string? LegacyDocId { get; set; }

    [ListItemColumn("LegalHolds", IsTypeNotSupported = true)]
    public List<ManagedMetadata>? LegalHolds { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_MarshAccountStatus", Inherited = true)]
    public Lookup? MarshAccountStatus { get; set; }

    [ListItemColumn("MarshComment")]
    public string? MarshComment { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_MarshEntity", Inherited = true)]
    public Lookup? MarshEntity { get; set; }

    [ListItemColumn("MarshSystemIDLookup", IsTypeNotSupported = true)]
    public ManagedMetadata? MarshSystemId { get; set; }

    [ListItemColumn("MSO_x0020_Office", IsTypeNotSupported = true)]
    public ManagedMetadata? MsoOffice { get; set; }

    [ListItemColumn("NDASecurity", IsTypeNotSupported = true)]
    public ManagedMetadata? NdaSecurity { get; set; }

    [ListItemColumn("ParentDocId")]
    public string? ParentDocId { get; set; }

    [ListItemColumn("Policy_x0020_Year")]
    public double? PolicyYear { get; set; }

    [ListItemColumn("ReceiveDate")]
    public DateTime? ReceiveDate { get; set; }

    [ListItemColumn("RescanEmailAddress")]
    public string? RescanEmailAddress { get; set; }

    [ListItemColumn("RetentionPermanent")]
    public bool? RetentionPermanent { get; set; }

    [ListItemColumn("ScanBatchName")]
    public string? ScanBatchName { get; set; }

    [ListItemColumn("ScanDate")]
    public DateTime? ScanDate { get; set; }

    [ListItemColumn("ScanDescription")]
    public string? ScanDescription { get; set; }

    [ListItemColumn("ScanState", IsTypeNotSupported = true)]
    public ManagedMetadata? ScanState { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_Segment", Inherited = true)]
    public Lookup? Segment { get; set; }

    [ListItemColumn("SendDate")]
    public DateTime? SendDate { get; set; }

    [ListItemColumn("Service_x0020_Year")]
    public double? ServiceYear { get; set; }

    [ListItemColumn("ServicingOffice", IsTypeNotSupported = true)]
    public ManagedMetadata? ServicingOffice { get; set; }

    [ListItemColumn("Subject")]
    public string? Subject { get; set; }

    [ListItemColumn("Title", IsIncludedByDefault = true)]
    public string? Title { get; set; }

    [ListItemColumn("TO")]
    public string? To { get; set; }

    [ListItemColumn("URL")]
    public Hyperlink? Url { get; set; }

    [ListItemColumn("VersionOnlyIndicator")]
    public bool? VersionOnlyIndicator { get; set; }

    [ListItemColumn("ClientReady")]
    public bool? VisibleToClient { get; set; }

    public static Expression<Func<AccountManagementV1, object>> All()
    {
        return s => new
        {
            s.AliasFlag,
            s.AttachmentIndicator,
            s.BatchId,
            s.Bcc,
            s.Cc,
            s.ChildDocIds,
            s.Description,
            s.DescriptionExtended,
            s.DocumentDate,
            s.DocumentType,
            s.EffectiveDate,
            s.ExpirationDate,
            s.ExtendedMetadata,
            s.ExternalSourceDocId,
            s.From,
            s.HiddenFlag,
            s.KeyValueAttribute,
            s.Keywords,
            s.LegacyDocId,
            s.LegalHolds,
            s.MarshComment,
            s.MarshSystemId,
            s.MsoOffice,
            s.NdaSecurity,
            s.ObjectId,
            s.ParentDocId,
            s.PolicyYear,
            s.ReceiveDate,
            s.RescanEmailAddress,
            s.RetentionPermanent,
            s.ScanBatchName,
            s.ScanDate,
            s.ScanDescription,
            s.ScanState,
            s.SendDate,
            s.ServiceYear,
            s.ServicingOffice,
            s.Subject,
            s.Title,
            s.To,
            s.Url,
            s.VersionOnlyIndicator,
            s.VisibleToClient
        };
    }

    public override Dictionary<string, object> Values<T>(
        Dictionary<string, string> columns,
        Expression<Func<T, object>>? fieldsToUpdate)
    {
        return ListItemBinder.GetValueSet(this, columns, fieldsToUpdate);
    }
}