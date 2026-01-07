using Microsoft.Graph.Models;
using S3WebApi.Interfaces;
using S3WebApi.Models;
using S3WebApi.Types;

namespace S3WebApi.Services
{
    public class SharePointService(ISharePointRepository sharePointRepository) : ISharePointService
    {
        private readonly ISharePointRepository _sharePointRepository = sharePointRepository ?? throw new ArgumentNullException(nameof(sharePointRepository));

        public async Task<List<MShareArchive>> GetFileMetaInfoAsync<T>(
            string contextUrl,
            InstitutionLibrary library,
            string itemPath,
            string location,
            string item,
            string client,
            string country,
            bool limitVersion,
            string contentType) where T : class, IDocumentModel
        {
            return await _sharePointRepository.GetFileMetaInfoAsync<T>(contextUrl, library, itemPath, location, item, client, country, limitVersion, contentType);
        }

        public async Task<(Stream FileStream, string FileName)> DownloadFileByUrlAsync(string fileUrl, string versionId, bool latest)
        {
            return await _sharePointRepository.DownloadFileByUrlAsync(fileUrl, versionId, latest);
        }

        public async Task<DriveItemVersionCollectionResponse> GetFileVersions(string fileUrl)
        {
            return await _sharePointRepository.GetFileVersions(fileUrl);
        }

        public async Task<string> GetContentType<T>(string fileUrl, InstitutionLibrary library, string item, string contextUrl) where T : class, IDocumentModel
        {
            return await _sharePointRepository.GetContentType<T>(fileUrl, library, item, contextUrl);
        }

        public async Task<List<SPGroupResponse>> GetSPGroupList(string siteUrl, string email)
        {
            return await _sharePointRepository.GetSPGroupList(siteUrl, email);
        }

        public async Task<bool> SoftDeleteDocument(string contextUrl, InstitutionLibrary library, string itemPath, string location, string item)
        {
            return await _sharePointRepository.SoftDeleteDocument(contextUrl, library, itemPath, location, item);
        }

        public async Task<string> StartArchive(ArchiveQueueDetails_Item doc, string country, bool limitVersion, bool deleteSource)
        {
            return await _sharePointRepository.StartArchive(doc, country, limitVersion, deleteSource);
        }
    }
}
