using Microsoft.SharePoint.Client.DocumentManagement;
using Newtonsoft.Json;
using System.Linq.Expressions;
using Validation;
using S3WebApi.Types;

namespace S3WebApi.Types
{
    public abstract class DocumentBaseV1 : IDocumentModel
    {
        protected DocumentBaseV1()
        {
        }

        protected DocumentBaseV1(IDocumentInfo documentInfo)
        {
            Requires.NotNull(documentInfo, nameof(documentInfo));

            DocumentInfo = documentInfo;
        }

        [ListItemColumn("@odata.etag", ReadOnly = true, ExcludeInClientFieldMapping = true)]
        public string? Etag { get; set; }

        [IgnoreBinding]
        public string? UniqueId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Etag))
                    return null;

                var values = Etag.TrimStart('"').TrimEnd('"').Split(',');

                return !values.Any() ? null : values.First();
            }
        }

        [IgnoreBinding]
        public IDocumentInfo? DocumentInfo { get; }

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

        [ListItemColumn("_VirusStatus", ReadOnly = true, ExcludeInClientFieldMapping = true)]
        public string? VirusStatus { get; set; }

        [ListItemColumn("_dlc_DocIdUrl", ReadOnly = true, ExcludeInClientFieldMapping = true)]
        public DocumentId? DlcDocId { get; set; }

        [ListItemColumn("FileSizeDisplay", ReadOnly = true, IsIncludedByDefault = true)]
        public int? FileSize { get; set; }

        [IgnoreBinding]
        public UserIdentityModel? CreatedBy { get; set; }

        [IgnoreBinding]
        public UserIdentityModel? LastModifiedBy { get; set; }

        public void SetObjectId()
        {
            if (DlcDocId == null)
            {
                ObjectId = Guid.NewGuid().ToString();

                return;
            }

            ObjectId = DlcDocId.Value;
        }

        public IDocumentModel BindProperties(
            Dictionary<string, Tuple<string, object>> properties,
            Func<string, string, string, string?, Guid> termsPredicate)
        {
            return MetadataUtility.BindProperties(this, properties, termsPredicate);
        }

        public IDocumentModel BindLookups(Dictionary<string, IReadOnlyCollection<int>> lookups)
        {
            return MetadataUtility.BindLookups(this, lookups);
        }

        public abstract Dictionary<string, object> Values<T>(
            Dictionary<string, string> columns,
            Expression<Func<T, object>>? fieldsToUpdate) where T : class, IListItemEntity;
    }
}
