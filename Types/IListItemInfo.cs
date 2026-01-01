namespace S3WebApi.Types
{
    public interface IListItemInfo
    {
        string ContextUrl { get; }
        string WebUrl { get; }
        string ListItemId { get; }
        string ParentListName { get; }
    }
}
