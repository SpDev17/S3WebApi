using Amazon.S3.Model;
using Microsoft.Graph.Models;
using S3WebApi.Helpers;
using S3WebApi.Interfaces;
using S3WebApi.Models;
using S3WebApi.Models.Docs;
using S3WebApi.Types;

namespace S3WebApi.ArchiveLayer;

public class Archive
{
    private readonly ISharePointService _sharePointService;
    private readonly IObjectStorageService _objectStorageService;
    private Serilog.ILogger _logger => Serilog.Log.ForContext<Archive>();
    private readonly IPostgresService _postgresService;

    public Archive(ISharePointService sharePointService, IObjectStorageService objectStorageService, IPostgresService postgresService)
    {
        _sharePointService = sharePointService;
        _objectStorageService = objectStorageService;
        _postgresService = postgresService;
    }

    public async Task<string> StartArchive(ArchiveQueueDetails_Item doc, string country, bool LimitVersion, bool DeleteSource)
    {
        try
        {
            Uri uri = new Uri(doc.FullPath);
            _logger.AddMethodName().Information("Starting Archive {url}", doc.FullPath);
            string[] segments = uri.Segments;
            string val = segments[4].Replace("%20", "");
            var clientId = segments[3].Replace("/", "");
            string location = segments[4].Replace("%20", " ").Replace("/", "");
            val = val.TrimEnd('/');
            string file = string.Empty;
            string clientSite = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : ":" + uri.Port)}";
            int i = 0;
            string client = string.Empty;
            foreach (string segment in segments.Take(segments.Length - 1))
            {
                if (i <= 3)
                {
                    clientSite += segment;
                }
                if (i > 4)
                {
                    file += segment.Replace("%20", " ");
                }
                i++;
            }

            file = file + Path.GetFileName(doc.FullPath); ;

            bool InstitutionLibraryFilter = Enum.IsDefined(typeof(InstitutionLibrary), val);
            string res = string.Empty;

            Enum.TryParse<InstitutionLibrary>(val, out InstitutionLibrary library);
            _logger.AddMethodName().Information("Filter Library {InstitutionLibraryFilter} for  {url}", doc.FullPath, InstitutionLibraryFilter);
            switch (val)
            {
                case "Placements":
                    res = await ProcessArchive<PlacementDocumentV1>(doc, file, clientSite, country, clientId, location, library, LimitVersion, DeleteSource);
                    break;
                case "AccountManagement":
                    res = await ProcessArchive<AccountManagementV1>(doc, file, clientSite, country, clientId, location, library, LimitVersion, DeleteSource);
                    break;
                case "Transactions":
                    res = await ProcessArchive<TransactionDocumentV1>(doc, file, clientSite, country, clientId, location, library, LimitVersion, DeleteSource);
                    break;
                case "Policies":
                    res = await ProcessArchive<PolicyDocumentV1>(doc, file, clientSite, country, clientId, location, library, LimitVersion, DeleteSource);
                    break;
                case "Fiduciary":
                    res = await ProcessArchive<FiduciaryDocumentV1>(doc, file, clientSite, country, clientId, location, library, LimitVersion, DeleteSource);
                    break;
                case "Claims":
                    res = await ProcessArchive<ClaimDocumentV1>(doc, file, clientSite, country, clientId, location, library, LimitVersion, DeleteSource);
                    break;
                case "Projects":
                    res = await ProcessArchive<ProjectDocumentV1>(doc, file, clientSite, country, clientId, location, library, LimitVersion, DeleteSource);
                    break;
                default:
                    await _postgresService.UpdateArchiveQueueStatusByIdAsync(doc.id, "The requested resource library was not found", "Failed");
                    throw new HttpStatusCodeException(404, "The requested resource library was not found.");
            }
            await _postgresService.UpdateArchiveQueueStatusByIdAsync(doc.id, "", "Success");
            return res;
        }
        catch (HttpStatusCodeException ex)
        {
            await _postgresService.UpdateArchiveQueueStatusByIdAsync(doc.id, "Error --" + ex.Message, "Failed");
            _logger.AddMethodName().Error($"Error to process file path : {0} exception : {1}", doc.FullPath, ex.Message);
            throw new HttpStatusCodeException(ex.StatusCode, ex.Message);
        }
    }

    public async Task<string> ProcessArchive<T>(ArchiveQueueDetails_Item doc, string file, string clientSite, string country, string clientId, string location, InstitutionLibrary library, bool LimitVersion, bool DeleteSource) where T : class, IDocumentModel
    {
        try
        {
            const int NoResult = 0;
            DriveItemVersionCollectionResponse files;
            try
            {
                files = await _sharePointService.GetFileVersions(doc.FullPath);
            }
            catch (Exception ex)
            {
                throw new HttpStatusCodeException(404, "The requested resource was not found.");
            }
            var contentType = await _sharePointService.GetContentType<T>(doc.FullPath, library, file, clientSite);
            List<MShareArchive> mDataList = await _sharePointService.GetFileMetaInfoAsync<T>(
                            clientSite,
                            library,
                                doc.FullPath, "/" + location + "/", file, clientId, country, LimitVersion, contentType);
            PutObjectResponse metadataResponse = null;
            bool duplicate = await _objectStorageService.ExistObjectAsync(country + "/" + clientId + "/" + location + "/" + file);
            if (duplicate)
            {
                _logger.AddMethodName().Information("File {FileUrl} already uploaded", doc.FullPath);
                return "document is already uploaded in S3 bucket";
            }
            if (files.Value.Count > 1 && LimitVersion)
            {
                var fileCount = files.Value.Count;
                var latestVersion = files.Value
                    .OrderByDescending(v => v.LastModifiedDateTime)
                    .FirstOrDefault();
                var delName = string.Empty;
                int index = mDataList.Count() - 1;
                foreach (var fileVersion in files.Value.AsEnumerable().Reverse())
                {
                    //----------------------File Download--------------------------------------------------
                    var (fileStm, name) = await _sharePointService.DownloadFileByUrlAsync(doc.FullPath, fileVersion.Id, fileVersion.Id == latestVersion.Id ? true : false);

                    using (fileStm)
                    {
                        //----------------------File Upload--------------------------------------------------
                        string docModifiedDate = string.Empty; // fileVersion.LastModifiedDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? null;
                        metadataResponse = await _objectStorageService.UploadObjectAsync(fileStm, country + "/" + clientId + "/" + location + "/" + file, doc.FullPath, docModifiedDate);
                        _logger.AddMethodName().Information("File {0} version {1} uploaded.", name, metadataResponse.VersionId);

                        var mData = mDataList[index];
                        mData.VersionId = metadataResponse.VersionId;
                        mData.IsPublishedVersion = index == 0 ? true : false;
                        mData.PublishedObjectId = index == 0 ? null : mDataList[(int)mDataList.Count() - 1].ObjectID;

                        var res = await _postgresService.InsertDataAsync(mData);
                        if (res <= NoResult)
                        {
                            _logger.AddMethodName().Information("Document {0} is deleted from SharePoint", name);
                            throw new HttpStatusCodeException(500, "Record is not inserted in the database.");
                        }
                    }
                    index--;
                }
                _logger.AddMethodName().Information("Metadata insert completed");
                if (DeleteSource)
                {
                    var chk = await _objectStorageService.ExistObjectAsync(country + "/" + clientId + "/" + location + "/" + file);
                    if (chk)
                    {
                        var delRes = await _sharePointService.SoftDeleteDocument(clientSite, library, doc.FullPath, location, delName);
                        if (!delRes)
                        {
                            throw new Exception($"getting error to file delete plz check file");
                        }
                        _logger.AddMethodName().Information("Document deleted from SharePoint");
                    }
                }

            }
            else
            {
                var latestVersion = files.Value
                    .OrderByDescending(v => v.LastModifiedDateTime)
                    .FirstOrDefault();
                //----------------------File Download--------------------------------------------------
                var (fileStm, name) = await _sharePointService.DownloadFileByUrlAsync(doc.FullPath, latestVersion.Id, true);

                using (fileStm)
                {
                    //----------------------File Upload--------------------------------------------------
                    string docModifiedDate = string.Empty; // latestVersion.LastModifiedDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? null;
                    metadataResponse = await _objectStorageService.UploadObjectAsync(fileStm, country + "/" + clientId + "/" + location + "/" + file, doc.FullPath, docModifiedDate);
                    _logger.AddMethodName().Information("File {0} uploaded.", name);

                    var mData = mDataList[mDataList.Count() - 1];
                    mData.VersionId = metadataResponse.VersionId;
                    mData.IsPublishedVersion = true;
                    mData.PublishedObjectId = null;

                    var res = await _postgresService.InsertDataAsync(mData);
                    if (res <= NoResult)
                    {
                        _logger.AddMethodName().Warning("Document deleted from SharePoint");
                        throw new HttpStatusCodeException(500, "Record is not inserted in the database.");
                    }

                    _logger.AddMethodName().Information("Metadata insert completed");
                    if (DeleteSource)
                    {
                        var chk = await _objectStorageService.ExistObjectAsync(country + "/" + clientId + "/" + location + "/" + file);
                        if (chk)
                        {
                            var delRes = await _sharePointService.SoftDeleteDocument(clientSite, library, doc.FullPath, location, name);
                            if (!delRes)
                            {
                                throw new Exception($"getting error to file delete plz check file");
                            }
                            _logger.AddMethodName().Information("Document deleted from SharePoint");
                        }
                    }
                }

            }

            return "document is uploaded";
        }
        catch (HttpStatusCodeException ex)
        {
            _logger.AddMethodName().Error("Http Status error caught processing archives exception : {0}", ex);
            throw new HttpStatusCodeException(404, ex.Message);
        }
    }
}
