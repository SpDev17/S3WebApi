﻿using Amazon.S3.Model;
using Microsoft.AspNetCore.Mvc;
using S3WebApi.Models;
using S3WebApi.Interfaces;

namespace S3WebApi.Services
{
    public class S3Service(IS3Repository s3Repository) : IObjectStorageService
    {
        private readonly IS3Repository _s3Repository = s3Repository;
        private Serilog.ILogger _logger => Serilog.Log.ForContext<S3Service>();

        public async Task<PutObjectResponse> UploadObjectAsync(Stream graphStream, string fileName, string fileUrl, string? docModifiedDate = null)
        {
            var response = await _s3Repository.S3UploadAsync(graphStream, fileName, fileUrl, docModifiedDate);
            _logger.ForContext("MethodName", nameof(UploadObjectAsync)).Information("Successfully completed S3UploadAsync for file {FileName} with URL {FileUrl}", fileName, fileUrl);
            return response;            
        }
        
        public async Task<IActionResult> DownloadStreamAsync(FileDownloadRequest fileReq, List<SPGroupResponse> permission)
        {
            _logger.ForContext("MethodName", nameof(DownloadStreamAsync)).Information("Starting DownloadFileStreamAsync for file {FileUrl}", fileReq.FileUrl);
            var result = await _s3Repository.DownloadFileStreamAsync(fileReq, permission);
            return result;
        }

        public async Task<bool> ExistObjectAsync(string fileName)
        {
            try
            {
                var checkFile = await _s3Repository.ExistObjectAsync(fileName);
                return checkFile;
            }
            catch (Exception ex)
            {
                _logger.ForContext("MethodName", nameof(ExistObjectAsync)).Error(ex, "ExistObjectAsync encountered an exception");
                throw;
            }
        }

        public async Task<List<string>> SearchFileAsync(string? keyword, string? extension)
        {
            _logger.ForContext("MethodName", nameof(SearchFileAsync)).Information("Starting SearchFileAsync with keyword {Keyword} and extension {Extension}", keyword, extension);
            try
            {
                var result = await _s3Repository.SearchFileAsync(keyword, extension);
                return result;
            }
            catch (Exception ex)
            {
                _logger.ForContext("MethodName", nameof(SearchFileAsync)).Error(ex, "SearchFileAsync encountered an exception");
                throw;
            }
        }
    }
}
