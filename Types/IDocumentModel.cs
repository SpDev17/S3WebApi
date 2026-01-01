namespace S3WebApi.Types
{
    public interface IDocumentModel : IListItemEntity
    {
        IDocumentInfo? DocumentInfo { get; }
        string? ObjectId { get; set; }
        string? UniqueId { get; }

        void SetObjectId();

        IDocumentModel BindProperties(
            Dictionary<string, Tuple<string, object>> properties,
            Func<string, string, string, string?, Guid> termsPredicate);

        IDocumentModel BindLookups(Dictionary<string, IReadOnlyCollection<int>> lookups);
    }
}
