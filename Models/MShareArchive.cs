namespace S3WebApi.Models;

public class MShareArchive
{
    public string ObjectID { get; set; }
    public string ClientID { get; set; }
    public string ContentType { get; set; }
    public string DocumentTypeMetadata { get; set; }
    public string ExtendedMetadata { get; set; }
    public string DateOfArchival {  get; set; }
    public string SecurityInfo { get; set; }
    public string DocumentArchiveLocation { get; set; }
    public string StorageLocation { get; set; }
    public string Country { get; set; }
    public string BucketName { get; set; }
    public string FileName { get; set; }
    public string LibraryName { get; set; }
    public string DocCreatedDate { get; set; }
    public string DocModifiedDate { get; set; }
    public string VersionId { get; set; }
    public bool IsPublishedVersion { get; set; }
    public string PublishedObjectId { get; set; }
}
