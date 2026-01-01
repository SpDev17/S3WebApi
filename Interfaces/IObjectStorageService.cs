using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using S3WebApi.Models;

namespace S3WebApi.Interfaces
{
    public interface IObjectStorageService
    {
        Task<PutObjectResponse> UploadObjectAsync(Stream graphStream, string fileName, string fileUrl, string? docModifiedDate = null);
        Task<IActionResult> DownloadStreamAsync(FileDownloadRequest fileReq, List<SPGroupResponse> permission);
        Task<bool> ExistObjectAsync(string fileName);
        Task<List<string>> SearchFileAsync(string? keyword, string? extension);
    }
}
