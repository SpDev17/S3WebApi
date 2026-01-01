namespace S3WebApi.Types;
public interface IDocumentSetInfo : IListItemInfo
{
    string FolderPath { get; }
    string DriveId { get; }
}