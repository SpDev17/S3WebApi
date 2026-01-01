namespace S3WebApi.Types;

public interface IListItemModel : IListItemEntity
{
    IListItemInfo? ListItemInfo { get; }
    bool RequireColumnDefinitions { get; }
}
