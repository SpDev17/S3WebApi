namespace S3WebApi.Types;

public interface IDocumentSetModel : IListItemEntity
{
    IDocumentSetInfo? DocumentSetInfo { get; }
    bool IsDocumentSetValid(IConfiguration configuration);
}