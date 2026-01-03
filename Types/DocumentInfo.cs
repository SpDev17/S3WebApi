namespace S3WebApi.Types;

public class DocumentInfo : IDocumentInfo
{
    public DocumentInfo(string filePath, string driveId, string itemPath, string contextUrl, string webUrl, string listItemId, string parentListName)
    {
        FilePath = filePath;
        DriveId = driveId;
        ItemPath = itemPath;
        ContextUrl = contextUrl;
        WebUrl = webUrl;
        ListItemId = listItemId;
        ParentListName = parentListName;
    }

    public string FilePath { get; }
    public string DriveId { get; }
    public string ItemPath { get; }
    public string ContextUrl { get; }
    public string WebUrl { get; set; }
    public string ListItemId { get; }
    public string ParentListName { get; }    
}