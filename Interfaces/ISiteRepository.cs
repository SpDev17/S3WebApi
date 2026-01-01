using System.Threading.Tasks;
using S3WebApi.Models;
using S3WebApi.Types;

namespace S3WebApi.Interfaces
{
    public interface ISiteRepository
    {
        Task<string> GetSiteIdAsync(string contextUrl);
        Task<string> GetLibraryAsync(string contextUrl, string siteId, InstitutionLibrary library);
    }
}
