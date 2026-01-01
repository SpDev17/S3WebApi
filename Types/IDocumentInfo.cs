namespace S3WebApi.Types
{
    public interface IDocumentInfo : IListItemInfo
    {
        string FilePath { get; }
        string DriveId { get; }
        string ItemPath { get; }
    }
}
