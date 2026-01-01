using S3WebApi.Interfaces;
using S3WebApi.Models;

namespace S3WebApi.Services
{
    public class PostgresService(IPostgresRepository postgresRepository) : IPostgresService
    {
        private readonly IPostgresRepository _postgresRepository = postgresRepository;

        public async Task<MetadataResult> GetMedatadataDynamicAsync(SearchCondition conditions, string permissions)
        {
            return await _postgresRepository.GetMedatadataDynamicAsync(conditions, permissions);
        }

        public async Task<List<dynamic>> GetArchiveQueueAsync(string ruleId, int limit)
        {
            var result = await _postgresRepository.GetArchiveQueueAsync(ruleId, limit);
            return result is List<dynamic> list ? list : result.ToList();
        }

        public async Task<int> GetArchiveQueueCountAsync(string ruleId)
        {
            return await _postgresRepository.GetArchiveQueueCountAsync(ruleId);
        }

        public async Task<bool> BulkUpsert_Documents_To_ArchiveQueue(IEnumerable<dynamic> queuedata)
        {
            return await _postgresRepository.BulkUpsert_Documents_To_ArchiveQueue(queuedata);
        }

        public async Task<IEnumerable<dynamic>> GetDataAsync(TableRequest table)
        {
            return await _postgresRepository.GetDataAsync(table);
        }

        public Task BulkUpsertDocumentDetailsAsync(List<DocumentDetails_Item> items, CancellationToken ct)
        {
            return _postgresRepository.BulkUpsertDocumentDetailsAsync(items, ct);
        }

        public async Task<MetadataResult> GetDocumentVersionsIdAsync(string id)
        {
            return await _postgresRepository.GetDocumentVersionsIdAsync(id);
        }

        public async Task<string> GetSiteURL(string clientId)
        {
            return await _postgresRepository.GetSiteURL(clientId);
        }

        public async Task<string> ValidateFileBeforeDownload(FileDownloadRequest fileReq)
        {
            return await _postgresRepository.ValidateFileBeforeDownload(fileReq);
        }

        public async Task<int> UpdateArchiveQueueStatusByIdAsync(Guid Id, string msg, string? status = null)
        {
            return await _postgresRepository.UpdateArchiveQueueStatusByIdAsync(Id, msg, status);
        }

        public async Task<int> InsertDataAsync(MShareArchive mData)
        {
            return await _postgresRepository.InsertDataAsync(mData);
        }

        public async Task<IEnumerable<dynamic>> GetUrlInfo(List<string> urls)
        {
            return await _postgresRepository.GetUrlInfo(urls);
        }
    }
}
