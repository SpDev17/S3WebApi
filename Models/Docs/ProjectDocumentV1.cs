using JetBrains.Annotations;
using S3WebApi.Types;
using System.Linq.Expressions;

namespace S3WebApi.Models.Docs;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ProjectDocumentV1 : DocumentBaseV1
{
    public ProjectDocumentV1()
    {
    }

    public ProjectDocumentV1(IDocumentInfo documentInfo) : base(documentInfo)
    {
    }

    // Excluded: CheckoutUser, CreatedBy, ModifiedBy

    [ListItemColumn("CompanyNoLookup_x003A_MarshAccountName", Inherited = true)]
    public Lookup? AccountName { get; set; }

    [ListItemColumn("BatchID")]
    public string? BatchId { get; set; }

    [ListItemColumn("CompanyNoLookup", Inherited = true)]
    public Lookup? CompanyNumber { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_Country", Inherited = true)]
    public Lookup? Country { get; set; }

    [ListItemColumn("_ExtendedDescription")]
    public string? DescriptionExtended { get; set; }

    [ListItemColumn("DocumentSetDescription", Inherited = true)]
    public string? DescriptionSet { get; set; }

    [ListItemColumn("ExtendedMetadata")]
    public string? ExtendedMetadata { get; set; }

    [ListItemColumn("ExternalSourceDocID")]
    public string? ExternalSourceDocId { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_FamilyCode", Inherited = true)]
    public Lookup? FamilyCode { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_GUAccountName", Inherited = true)]
    public Lookup? GuAccountName { get; set; }

    // ReSharper disable once StringLiteralTypo
    [ListItemColumn("CompanyNoLookup_x003A_GUDUNSNumber", Inherited = true)]
    public Lookup? GuDunsNumber { get; set; }

    [ListItemColumn("Keywords", IsIncludedByDefault = true)]
    public string? Keywords { get; set; }

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

    [ListItemColumn("ProjectCloseDate", Inherited = true)]
    public DateTime? ProjectCloseDate { get; set; }

    [ListItemColumn("ProjectEndDate", Inherited = true)]
    public DateTime? ProjectEndDate { get; set; }

    [ListItemColumn("ProjectName", Inherited = true)]
    public string? ProjectName { get; set; }

    [ListItemColumn("ProjectNo", Inherited = true)]
    public string? ProjectNumber { get; set; }

    [ListItemColumn("ProjectOpenDate", Inherited = true)]
    public DateTime? ProjectOpenDate { get; set; }

    [ListItemColumn("ProjectStartDate", Inherited = true)]
    public DateTime? ProjectStartDate { get; set; }

    [ListItemColumn("CompanyNoLookup_x003A_Segment", Inherited = true)]
    public Lookup? Segment { get; set; }

    [ListItemColumn("Title", IsIncludedByDefault = true)]
    public string? Title { get; set; }

    public static Expression<Func<ProjectDocumentV1, object>> All()
    {
        return s => new
        {
            s.BatchId,
            s.DescriptionExtended,
            s.ExtendedMetadata,
            s.ExternalSourceDocId,
            s.Keywords,
            s.LegalHolds,
            s.MarshComment,
            s.MarshSystemId,
            s.MsoOffice,
            s.ObjectId,
            s.Title
        };
    }

    public override Dictionary<string, object> Values<T>(
        Dictionary<string, string> columns,
        Expression<Func<T, object>>? fieldsToUpdate)
    {
        return ListItemBinder.GetValueSet(this, columns, fieldsToUpdate);
    }
}
