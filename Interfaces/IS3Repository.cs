using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using S3WebApi.Models;

namespace S3WebApi.Interfaces
{
    public interface IS3Repository
    {
        Task<PutObjectResponse> S3UploadAsync(Stream graphStream, string fileName, string fileUrl, string? docModifiedDate = null);
        Task<IActionResult> DownloadFileStreamAsync(FileDownloadRequest fileReq, List<SPGroupResponse> permission);
        Task<bool> ExistObjectAsync(string fileName);
        Task<List<string>> SearchFileAsync(string? keyword, string? extension);
    }
}