using Microsoft.AspNetCore.Mvc;
using S3WebApi.Models;
using S3WebApi.ArchiveLayer;
using System.Collections.Concurrent;
using S3WebApi.Constent;
using S3WebApi.Helpers;
using S3WebApi.Interfaces;
using S3WebApi.APIRoutes;

namespace S3WebApi.Controllers
{
    [ApiController]
    [Route(ApiRoutes.ControllerRoute.Controller)]
    //[MShareAuthorize]
    public class SharePointController : Controller
    {
        private readonly ISharePointService _sharePointService;
        private readonly IObjectStorageService _s3Services;
        private readonly Archive _archive;
        private readonly IPostgresService _postgresService;
        private Serilog.ILogger _logger => Serilog.Log.ForContext<SharePointController>();

        private readonly IConfiguration _configuration;

        public SharePointController(ISharePointService sharePointService, IObjectStorageService s3Services, Archive archive, IPostgresService postgresService, IConfiguration configuration)
        {
            _sharePointService = sharePointService;
            _s3Services = s3Services;
            _archive = archive;
            _postgresService = postgresService;
            _configuration = configuration;
        }

        [HttpPost("ArchiveObject")]
        public async Task<IActionResult> ArchiveObject([FromBody] S3FileRequest request)
        {
            try
            {
                _logger.AddMethodName().Information("Entered ArchiveObject method with request : {@request}.", request);
                var responses = new ConcurrentBag<object>();
                var exceptions = new ConcurrentBag<string>();
                if (request.Urls.Count == 0)
                {
                    return BadRequest(new { success = false, message = "No urls for archive." });
                }
                bool exists = CountryConstants.Countries.Any(c => c.Equals(request.Country, System.StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    return BadRequest($"Country '{request.Country}' is not supported.");
                }
                #region get url info from db
                IEnumerable<dynamic> dbResponse = await _postgresService.GetUrlInfo(request.Urls);
                List<ArchiveQueueDetails_Item> items = await Mapper.GetTypeObjectList<ArchiveQueueDetails_Item>(dbResponse);
                await Parallel.ForEachAsync(request.Urls, async (url, cancellationToken) =>
                               {
                                   try
                                   {
                                       if (items != null && items.Count > 0)
                                       {
                                           var queDoc = items
                                               .Where(x => x.FullPath != null && x.FullPath == url)
                                               .ToList().FirstOrDefault();
                                           if (queDoc != null)
                                           {
                                               if (string.Equals(queDoc.Status, "success", StringComparison.OrdinalIgnoreCase))
                                               {
                                                   responses.Add(new ArchiveResponse
                                                   {
                                                       ID = queDoc.id.ToString(),
                                                       Url = queDoc.FullPath!,
                                                       ReturnCode = 201,
                                                       Message = "document is already inserted in database",
                                                       Error = string.Empty
                                                   });
                                                   return;
                                               }
                                               if (string.Equals(queDoc.Status, "restricted", StringComparison.OrdinalIgnoreCase))
                                               {
                                                   responses.Add(new ArchiveResponse
                                                   {
                                                       ID = queDoc.id.ToString(),
                                                       Url = queDoc.FullPath!,
                                                       ReturnCode = 405,
                                                       Message = "document type is not allowed to archived",
                                                       Error = string.Empty
                                                   });
                                                   return;
                                               }
                                               try
                                               {
                                                   if (CommonExtensions.IsDocumentTypeNotAllowedToArchived(queDoc.FullPath, _configuration["RestrictedExtension"]))
                                                   {
                                                       await _postgresService.UpdateArchiveQueueStatusByIdAsync(queDoc.id, "Document type is not allowed to archived", "Restricted");
                                                       responses.Add(new ArchiveResponse
                                                       {
                                                           ID = queDoc.id.ToString(),
                                                           Url = queDoc.FullPath,
                                                           ReturnCode = 405,
                                                           Message = "document type is not allowed to archived",
                                                           Error = ""
                                                       });
                                                   }
                                                   else
                                                   {
                                                       var result = await Task.Run(async () => await _archive.StartArchive(queDoc, request.Country, request.LimitVersion, request.DeleteSource), cancellationToken);
                                                       responses.Add(new ArchiveResponse
                                                       {
                                                           ID = queDoc.id.ToString(),
                                                           Url = queDoc.FullPath,
                                                           ReturnCode = 200,
                                                           Message = result,
                                                           Error = string.Empty
                                                       });
                                                   }
                                               }
                                               catch (HttpStatusCodeException ex)
                                               {
                                                   responses.Add(new ArchiveResponse
                                                   {
                                                       ID = queDoc.id.ToString(),
                                                       Url = queDoc.FullPath,
                                                       ReturnCode = ex.StatusCode,
                                                       Message = "document is not uploaded",
                                                       Error = ex.Message
                                                   });
                                               }
                                           }
                                           else
                                           {
                                               responses.Add(new ArchiveResponse
                                               {
                                                   ID = "",
                                                   Url = url,
                                                   ReturnCode = 404,
                                                   Message = "document not found",
                                                   Error = "document not found"
                                               });
                                           }
                                       }
                                       else
                                       {
                                           responses.Add(new ArchiveResponse
                                           {
                                               ID = "",
                                               Url = url,
                                               ReturnCode = 404,
                                               Message = "document not found",
                                               Error = "document not found"
                                           });
                                       }
                                   }
                                   catch (Exception ex)
                                   {
                                       _logger.AddMethodName().Error("IsDocumentTypeNotAllowedToArchived encountered an exception : {0}", ex);
                                       exceptions.Add($"{ex.Message}");
                                   }
                               });

                #endregion
                return Json(new
                {
                    apiCallStatus = 200,
                    responses = responses,
                    apiMessage = string.Empty
                });
            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error("ArchiveObject encountered an exception : {0}", ex);
                return Json(new { apiCallStatus = 500, responses = new string[] { }, apiMessage = "Message:" + ex.Message });
            }
        }
    }
}
