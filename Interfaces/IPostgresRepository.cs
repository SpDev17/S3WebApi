using System.Data;
using S3WebApi.Models;

namespace S3WebApi.Interfaces;

public interface IPostgresRepository
{
    Task<MetadataResult> GetMedatadataDynamicAsync(SearchCondition condition, string permission);
    Task<int> UpdateArchiveQueueStatusAsync(string message, string url, string? status);
    Task<int> CreateDocDownloadLogAsync(FileDownloadRequest reqLog);
    Task<int> InsertDataAsync(MShareArchive mData);
    Task<string> GetSiteURL(string client_Id);

    Task<IEnumerable<dynamic>> GetArchiveQueueAsync(string jobId, int limit);
    Task<int> GetArchiveQueueCountAsync(string jobId);
    Task<IEnumerable<dynamic>> GetDataAsync(TableRequest table);
    Task<int> InsertArchiveQueueDetailsAsync(IDictionary<string, object> data);
    Task<bool> BulkInsert_Documents_To_ArchiveQueue(IEnumerable<dynamic> data);
    Task<bool> BulkUpsert_Documents_To_ArchiveQueue(IEnumerable<dynamic> data);
    Task<IEnumerable<dynamic>> GetUrlInfo(List<string> urls);
     Task<DataTable> GetTableDataAsync(TableRequest table);
    Task BulkUpsertDocumentDetailsAsync(IEnumerable<DocumentDetails_Item> items, CancellationToken ct = default);
    Task<int> UpdateArchiveQueueStatusByIdAsync(Guid Id, string msg, string? status);
    Task<MetadataResult> GetDocumentVersionsIdAsync(string Id);
    Task<string> ValidateFileBeforeDownload(FileDownloadRequest fileReq);
    Task<bool> GetPermissionByURL(FileDownloadRequest fileReq, List<SPGroupResponse> permissions);    
}
