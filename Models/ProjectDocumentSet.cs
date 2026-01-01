using JetBrains.Annotations;
using S3WebApi.Types;
using System.Linq.Expressions;

namespace S3WebApi.Models;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ProjectDocumentSet : DocumentSetBase
{
    public ProjectDocumentSet()
    {
    }

    public ProjectDocumentSet(IDocumentSetInfo documentSetInfo) : base(documentSetInfo)
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

    [ListItemColumn("ProjectCloseDate")]
    public DateTime? ProjectCloseDate { get; set; }

    [ListItemColumn("ProjectEndDate")]
    public DateTime? ProjectEndDate { get; set; }

    [ListItemColumn("ProjectName", IsIncludedByDefault = true)]
    public string? ProjectName { get; set; }

    [ListItemColumn("ProjectNo", IsIncludedByDefault = true)]
    public string? ProjectNumber { get; set; }

    [ListItemColumn("ProjectOpenDate")]
    public DateTime? ProjectOpenDate { get; set; }

    [ListItemColumn("ProjectStartDate")]
    public DateTime? ProjectStartDate { get; set; }

    [ListItemColumn("Title", IsIncludedByDefault = true)]
    public string? Title { get; set; }

    public override bool IsDocumentSetValid(IConfiguration configuration)
    {
        var contentTypes =
            configuration
                .GetSection(DOCUMENT_SET_CONTENT_TYPE_SECTION)
                .GetSection(nameof(ProjectDocumentSet))
                .Get<List<string>>();

        return
            CompanyNumber != null &&
            Country != null &&
            Coverages != null &&
            Coverages.Any() &&
            !string.IsNullOrWhiteSpace(ProjectNumber) &&
            !string.IsNullOrWhiteSpace(ProjectName) &&
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
