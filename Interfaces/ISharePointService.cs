using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Graph.Models;
using S3WebApi.Models;
using S3WebApi.Types;

namespace S3WebApi.Interfaces
{
    public interface ISharePointService
    {
        Task<List<MShareArchive>> GetFileMetaInfoAsync<T>(
            string contextUrl,
            InstitutionLibrary library,
            string itemPath,
            string location,
            string item,
            string client,
            string country,
            bool limitVersion,
            string contentType) where T : class, IDocumentModel;

        Task<(Stream FileStream, string FileName)> DownloadFileByUrlAsync(string fileUrl, string versionId, bool latest);

        Task<DriveItemVersionCollectionResponse> GetFileVersions(string fileUrl);

        Task<string> GetContentType<T>(string fileUrl, InstitutionLibrary library, string item, string contextUrl) where T : class, IDocumentModel;

        Task<List<SPGroupResponse>> GetSPGroupList(string siteUrl, string email);

        Task<bool> SoftDeleteDocument(string contextUrl, InstitutionLibrary library, string itemPath, string location, string item);
    }
}
