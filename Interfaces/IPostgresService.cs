using Microsoft.AspNetCore.SignalR.Protocol;
using S3WebApi.Models;
using S3WebApi.Models.Common;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace S3WebApi.Interfaces
{
    public interface IPostgresService
    {
        Task<MetadataResult> GetMedatadataDynamicAsync(SearchCondition conditions, string permissions);
        Task<List<dynamic>> GetArchiveQueueAsync(string ruleId, int limit);
        Task<int> GetArchiveQueueCountAsync(string ruleId);
        Task<bool> BulkUpsert_Documents_To_ArchiveQueue(IEnumerable<dynamic> queuedata);
        Task<IEnumerable<dynamic>> GetDataAsync(TableRequest table);
        Task BulkUpsertDocumentDetailsAsync(List<DocumentDetails_Item> items, CancellationToken ct);
        Task<MetadataResult> GetDocumentVersionsIdAsync(string id);
        Task<string> GetSiteURL(string clientId);
        Task<string> ValidateFileBeforeDownload(FileDownloadRequest fileReq);        
        Task<int> UpdateArchiveQueueStatusByIdAsync(Guid Id, string msg, string? status);
        Task<int> InsertDataAsync(MShareArchive mData);
        Task<IEnumerable<dynamic>> GetUrlInfo(List<string> urls);
    }
}
