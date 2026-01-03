using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using Microsoft.AspNetCore.Mvc;
using S3WebApi.DMSAuth;
using S3WebApi.Models;
using System.Net;
using Tag = Amazon.S3.Model.Tag;
using MongoDB.Driver;
using S3WebApi.Interfaces;

namespace S3WebApi.Repository
{
    public class S3Repository : IS3Repository
    {
        private readonly IAmazonS3 _s3Client;
        //private readonly AwsS3Settings _settings;
        private readonly AssumeRoleRequestDto _settings;
        private readonly IPostgresRepository _postgresRepository;
        private readonly IConfiguration _configuration;
        private readonly AuthSecret _authSecret;

        private Serilog.ILogger _logger => Serilog.Log.ForContext<S3Repository>();

        public S3Repository(IConfiguration configuration, AuthSecret authSecret, IPostgresRepository postgresRepository)
        {
            _settings = configuration.GetSection("AwsS3PVCSettings").Get<AssumeRoleRequestDto>();
            _authSecret = authSecret;
            var region = RegionEndpoint.GetBySystemName(_settings.Region);
            _authSecret.Certificates.TryGetValue(_settings.AccessKey, out var accessKeyString);
            _authSecret.Certificates.TryGetValue(_settings.SecretKey, out var secretKeyString);
            _authSecret.Certificates.TryGetValue(_settings.RoleArn, out var roleArnString);
            _postgresRepository = postgresRepository;
            #region S3 initialize
            _s3Client = new AmazonS3Client(accessKeyString, secretKeyString, region);

            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", accessKeyString);
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", secretKeyString);

            var roleArn = WebUtility.UrlDecode(roleArnString);
            var sessionName = Guid.NewGuid().ToString();
            var vpceUrl = WebUtility.UrlDecode(_settings.EndpointUrl);
            var bucketName = WebUtility.UrlDecode(_settings.BucketName);
            string prefix = null;
            string continuationToken = null;
            var credentials = new BasicAWSCredentials(accessKeyString, secretKeyString);
            var clientS = new AmazonSecurityTokenServiceClient(credentials, RegionEndpoint.GetBySystemName(_settings.Region));
            var command = new AssumeRoleRequest();
            command.RoleArn = roleArn;
            command.RoleSessionName = sessionName;
            command.DurationSeconds = 3600;
            var assumedRole = clientS.AssumeRoleAsync(command).GetAwaiter().GetResult();

            var stsConfig = new AmazonSecurityTokenServiceConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region)
            };

            var sessionAwsCreds = new SessionAWSCredentials(
                 assumedRole.Credentials.AccessKeyId,
                assumedRole.Credentials.SecretAccessKey,
                assumedRole.Credentials.SessionToken);

            var s3Config = new AmazonS3Config();
            s3Config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region),
                //ServiceURL = vpceUrl
            };
            _s3Client = new AmazonS3Client(sessionAwsCreds, s3Config);
            #endregion
        }        

        public async Task<PutObjectResponse> S3UploadAsync(Stream graphStream, string fileName, string fileUrl, string? docModifiedDate = null)
        {
            Stream uploadStream = graphStream;
            PutObjectResponse response = new PutObjectResponse();

            try
            {
                _authSecret.Certificates.TryGetValue(_settings.AccessKey, out var accessKeyString);
                _authSecret.Certificates.TryGetValue(_settings.SecretKey, out var secretKeyString);
                _authSecret.Certificates.TryGetValue(_settings.RoleArn, out var roleArnString);

                Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", accessKeyString);
                Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", secretKeyString);

                var roleArn = WebUtility.UrlDecode(roleArnString);
                var sessionName = Guid.NewGuid().ToString();
                var vpceUrl = WebUtility.UrlDecode(_settings.EndpointUrl);
                var bucketName = WebUtility.UrlDecode(_settings.BucketName);
                string prefix = null;
                string continuationToken = null;

                var stsConfig = new AmazonSecurityTokenServiceConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region)
                };
                var credentials = new BasicAWSCredentials(accessKeyString, secretKeyString);
                var clientS = new AmazonSecurityTokenServiceClient(credentials, RegionEndpoint.GetBySystemName(_settings.Region));
                var command = new AssumeRoleRequest();
                command.RoleArn = roleArn;
                command.RoleSessionName = sessionName;
                command.DurationSeconds = 3600;
                // command.WebIdentityToken = vpceUrl;
                var assumedRole = await clientS.AssumeRoleAsync(command);

                var sessionAwsCreds = new SessionAWSCredentials(
                assumedRole.Credentials.AccessKeyId,
                assumedRole.Credentials.SecretAccessKey,
                assumedRole.Credentials.SessionToken);

                var s3Config = new AmazonS3Config();
                // If your S3 also needs to go via a VPC endpoint, configure AmazonS3Config.ServiceURL similarly.
                s3Config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region),
                    //ServiceURL = vpceUrl
                };
                using var s3 = new AmazonS3Client(sessionAwsCreds, s3Config);

                if (!graphStream.CanSeek)
                {
                    // Copy network stream to MemoryStream
                    var memStream = new MemoryStream();
                    await graphStream.CopyToAsync(memStream);
                    memStream.Position = 0;
                    uploadStream = memStream;
                }
                else
                {
                    graphStream.Position = 0; // reset if possible
                }
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = fileName,
                    InputStream = uploadStream
                };
                //var tagSet = new List<Tag>
                //{
                //    new() { Key = "DocCreatedDate", Value = DateTime.Now.ToString() }
                //};
                //if (!string.IsNullOrEmpty(docModifiedDate))
                //{
                //    if (!string.IsNullOrEmpty(docModifiedDate))
                //    {
                //        tagSet.Add(new Tag { Key = "DocModifiedDate", Value = docModifiedDate });
                //    }
                //}
                //putRequest.TagSet = tagSet;

                response = await s3.PutObjectAsync(putRequest).ConfigureAwait(false);
                _logger.AddMethodName().Information("Successfully completed S3UploadAsync for file {FileName} with URL {FileUrl}", fileName, fileUrl);
                return response;
            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error("Error in S3UploadAsync for file {0} with URL {1} and exception : {2}", fileName, fileUrl, ex);
                await _postgresRepository.UpdateArchiveQueueStatusAsync("Error in DownloadFileByUrlAsync() message : " + ex.Message, fileUrl, null);
                throw new Exception(ex.Message);
            }
            finally
            {
                if (uploadStream != graphStream) // dispose only if we created a copy
                    uploadStream.Dispose();
            }
        }        

        public async Task<IActionResult> DownloadFileStreamAsync(FileDownloadRequest fileReq, List<SPGroupResponse> permission)
        {
            _logger.AddMethodName().Information("Starting DownloadFileStreamAsync for file {FileUrl}", fileReq.FileUrl);
            bool res = await _postgresRepository.GetPermissionByURL(fileReq, permission);
            if (!res)
            {
                _logger.AddMethodName().Warning("DownloadFileStreamAsync You don't have permission to download!");
                return new ObjectResult(new { errorcode = 401, message = "You don't have permission to download!" })
                {
                    StatusCode = 401
                };
            }
            try
            {
                _authSecret.Certificates.TryGetValue(_settings.AccessKey, out var accessKeyString);
                _authSecret.Certificates.TryGetValue(_settings.SecretKey, out var secretKeyString);
                _authSecret.Certificates.TryGetValue(_settings.RoleArn, out var roleArnString);

                Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", accessKeyString);
                Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", secretKeyString);

                var roleArn = WebUtility.UrlDecode(roleArnString);
                var sessionName = Guid.NewGuid().ToString();
                var vpceUrl = WebUtility.UrlDecode(_settings.EndpointUrl);
                var bucketName = WebUtility.UrlDecode(_settings.BucketName);
                string prefix = null;
                string continuationToken = null;

                var stsConfig = new AmazonSecurityTokenServiceConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region)
                };
                _logger.AddMethodName().Information("stsConfig object is created");

                var credentials = new BasicAWSCredentials(accessKeyString, secretKeyString);
                var clientS = new AmazonSecurityTokenServiceClient(credentials, RegionEndpoint.GetBySystemName(_settings.Region));
                var command = new AssumeRoleRequest();
                command.RoleArn = roleArn;
                command.RoleSessionName = sessionName;
                command.DurationSeconds = 3600;

                var assumedRole = await clientS.AssumeRoleAsync(command);
                _logger.AddMethodName().Information("assumedRole object is created");

                var sessionAwsCreds = new SessionAWSCredentials(
                assumedRole.Credentials.AccessKeyId,
                assumedRole.Credentials.SecretAccessKey,
                assumedRole.Credentials.SessionToken);

                // If your S3 also needs to go via a VPC endpoint, configure AmazonS3Config.ServiceURL similarly.
                var s3Config = new AmazonS3Config();
                s3Config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region),
                    //ServiceURL = vpceUrl
                };
                _logger.AddMethodName().Information("s3Config object is created");

                using var s3 = new AmazonS3Client(sessionAwsCreds, s3Config);
                try
                {
                    var metadataRequest = new GetObjectMetadataRequest
                    {
                        BucketName = bucketName,
                        Key = fileReq.FileUrl
                    };
                    var metadataResponse = await s3.GetObjectMetadataAsync(metadataRequest).ConfigureAwait(false);
                    _logger.AddMethodName().Information("checked metadata in S3 bucket");

                    if (metadataResponse != null)
                    {
                        // If no exception, file exists
                        var getObjectRequest = new GetObjectRequest
                        {
                            BucketName = bucketName,
                            Key = fileReq.FileUrl
                        };
                        _logger.AddMethodName().Information("getting stream from S3 bucket");

                        using var getObjectResponse = await s3.GetObjectAsync(getObjectRequest);

                        var memoryStream = new MemoryStream();
                        await getObjectResponse.ResponseStream.CopyToAsync(memoryStream);
                        memoryStream.Position = 0;

                        return new FileStreamResult(memoryStream, getObjectResponse.Headers.ContentType ?? "application/octet-stream")
                        {
                            FileDownloadName = fileReq.FileName
                        };
                    }
                    else
                    {
                        throw new Exception($"File {fileReq.FileName} does not exist in bucket {_settings.BucketName}.");
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            catch (AmazonS3Exception s3Ex) when (s3Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // File does not exist, proceed to upload
                _logger.AddMethodName().Information("File {FileName} does not exist in bucket {BucketName}.", fileReq.FileName, _settings.BucketName);
                throw s3Ex;
            }
        }

        public async Task<bool> ExistObjectAsync(string fileName)
        {
            try
            {
                _authSecret.Certificates.TryGetValue(_settings.AccessKey, out var accessKeyString);
                _authSecret.Certificates.TryGetValue(_settings.SecretKey, out var secretKeyString);
                _authSecret.Certificates.TryGetValue(_settings.RoleArn, out var roleArnString);

                Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", accessKeyString);
                Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", secretKeyString);
                var roleArn = WebUtility.UrlDecode(roleArnString);
                var sessionName = Guid.NewGuid().ToString();
                var vpceUrl = WebUtility.UrlDecode(_settings.EndpointUrl);
                var bucketName = WebUtility.UrlDecode(_settings.BucketName);
                string prefix = null;
                string continuationToken = null;

                var stsConfig = new AmazonSecurityTokenServiceConfig
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region)
                };
                var credentials = new BasicAWSCredentials(accessKeyString, secretKeyString);
                var clientS = new AmazonSecurityTokenServiceClient(credentials, RegionEndpoint.GetBySystemName(_settings.Region));
                var command = new AssumeRoleRequest();
                command.RoleArn = roleArn;
                command.RoleSessionName = sessionName;
                command.DurationSeconds = 3600;
                // command.WebIdentityToken = vpceUrl;
                var assumedRole = await clientS.AssumeRoleAsync(command);

                var sessionAwsCreds = new SessionAWSCredentials(
                assumedRole.Credentials.AccessKeyId,
                assumedRole.Credentials.SecretAccessKey,
                assumedRole.Credentials.SessionToken);

                // If your S3 also needs to go via a VPC endpoint, configure AmazonS3Config.ServiceURL similarly.
                var s3Config = new AmazonS3Config();
                s3Config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(_settings.Region),
                    //ServiceURL = vpceUrl
                };

                using var s3 = new AmazonS3Client(sessionAwsCreds, s3Config);
                // Check if the file exists in S3
                bool checkFile = false;
                try
                {
                    var metadataRequest = new GetObjectMetadataRequest
                    {
                        BucketName = bucketName,
                        Key = fileName
                    };
                    var metadataResponse = await s3.GetObjectMetadataAsync(metadataRequest).ConfigureAwait(false);
                    checkFile = true;

                    // If no exception, file exists
                    _logger.AddMethodName().Information("File {FileName} already exists in bucket {BucketName}. Skipping upload.", fileName, bucketName);
                    //return "File already exists in S3. Upload skipped.";
                }
                catch (AmazonS3Exception s3Ex) when (s3Ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // File does not exist, proceed to upload
                    _logger.AddMethodName().Information("File {FileName} does not exist in bucket {BucketName}.", fileName, bucketName);
                }
                return checkFile;
            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error("ExistObjectAsync encountered an exception : {0}", ex);
                throw ex;
            }
        }        

        public async Task<List<string>> SearchFileAsync(string? keyword, string? extension)
        {
            _logger.AddMethodName().Information("Starting SearchFileAsync with keyword {Keyword} and extension {Extension}", keyword, extension);
            try
            {
                var request = new ListObjectsV2Request
                {
                    BucketName = _settings.BucketName
                };

                var response = await _s3Client.ListObjectsV2Async(request);

                var files = response.S3Objects.Select(o => o.Key).Where(Key => (string.IsNullOrEmpty(keyword) || Key.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                && (string.IsNullOrEmpty(extension) || Key.EndsWith(extension, StringComparison.OrdinalIgnoreCase))).ToList();

                _logger.AddMethodName().Information("SearchFileAsync found {FileCount} files", files.Count);
                return files;
            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error(ex, "Error in SearchFileAsync with keyword {Keyword} and extension {Extension}", keyword, extension);
                throw;
            }
        }

    }
}
