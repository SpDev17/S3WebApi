using System.Linq.Expressions;

namespace S3WebApi.Types
{
    public interface IListItemEntity
    {
        Dictionary<string, object> Values<T>(
        Dictionary<string, string> columns,
        Expression<Func<T, object>>? fieldsToUpdate) where T : class, IListItemEntity;
    }
}
