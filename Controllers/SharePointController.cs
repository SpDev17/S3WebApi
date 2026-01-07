using Microsoft.AspNetCore.Mvc;
using S3WebApi.Models;
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
    public class SharePointController : ControllerBase
    {
        private readonly ISharePointService _sharePointService;
        private readonly IPostgresService _postgresService;
        private Serilog.ILogger _logger => Serilog.Log.ForContext<SharePointController>();

        private readonly IConfiguration _configuration;

        public SharePointController(ISharePointService sharePointService, IPostgresService postgresService, IConfiguration configuration)
        {
            _sharePointService = sharePointService;
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
                if (request.Urls.Count == 0)
                {
                    return ApiResponseHelper.BadRequest("No urls for archive!");
                }
                bool exists = CountryConstants.Countries.Any(c => c.Equals(request.Country, System.StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    return ApiResponseHelper.BadRequest($"Country '{request.Country}' is not supported.");
                }
                #region get url info from db
                IEnumerable<dynamic> dbResponse = await _postgresService.GetUrlInfo(request.Urls);
                List<ArchiveQueueDetails_Item> items = await Mapper.GetTypeObjectList<ArchiveQueueDetails_Item>(dbResponse);
                await Parallel.ForEachAsync(request.Urls, async (url, cancellationToken) =>
                    {
                        if (items != null && items.Count > 0)
                        {
                            var queDoc = items
                                .Where(x => x.FullPath != null && x.FullPath == url)
                                .ToList().FirstOrDefault();
                            if (queDoc != null)
                            {
                                ArchiveResponse resp = new()
                                {
                                    ID = queDoc.id.ToString(),
                                    Url = queDoc.FullPath!
                                };
                                if (string.Equals(queDoc.Status, "success", StringComparison.OrdinalIgnoreCase))
                                {
                                    resp.ReturnCode = 201;
                                    resp.Message = "document is already inserted in database";
                                    resp.Error = string.Empty;
                                    responses.Add(resp);
                                    return;
                                }
                                if (string.Equals(queDoc.Status, "restricted", StringComparison.OrdinalIgnoreCase))
                                {
                                    resp.ReturnCode = 405;
                                    resp.Message = "document type is not allowed to archived";
                                    resp.Error = string.Empty;
                                    responses.Add(resp);
                                    return;
                                }
                                try
                                {
                                    if (CommonExtensions.IsDocumentTypeNotAllowedToArchived(queDoc.FullPath, _configuration["RestrictedExtension"]))
                                    {
                                        await _postgresService.UpdateArchiveQueueStatusByIdAsync(queDoc.id, "Document type is not allowed to archived", "Restricted");
                                        resp.ReturnCode = 405;
                                        resp.Message = "document type is not allowed to archived";
                                        resp.Error = "";
                                        responses.Add(resp);
                                    }
                                    else
                                    {
                                        var result = await Task.Run(async () => await _sharePointService.StartArchive(queDoc, request.Country, request.LimitVersion, request.DeleteSource), cancellationToken);
                                        resp.ReturnCode = 200;
                                        resp.Message = result;
                                        resp.Error = string.Empty;
                                        responses.Add(resp);
                                    }
                                }
                                catch (HttpStatusCodeException ex)
                                {
                                    ArchiveResponse respCatch = new()
                                    {
                                        ID = queDoc.id.ToString(),
                                        Url = queDoc.FullPath,
                                        ReturnCode = ex.StatusCode,
                                        Message = "document is not uploaded",
                                        Error = ex.Message
                                    };
                                    responses.Add(respCatch);
                                }
                            }
                            else
                            {
                                ArchiveResponse resp = new()
                                {
                                    ID = "",
                                    Url = url,
                                    ReturnCode = 404,
                                    Message = "document not found",
                                    Error = "document not found"
                                };
                                responses.Add(resp);
                            }
                        }
                        else
                        {
                            ArchiveResponse resp = new()
                            {
                                ID = "",
                                Url = url,
                                ReturnCode = 404,
                                Message = "document not found",
                                Error = "document not found"
                            };
                            responses.Add(resp);
                        }
                    });

                #endregion
                return new JsonResult(new
                {
                    apiCallStatus = 200,
                    responses = responses,
                    apiMessage = string.Empty
                });
            }
            catch (Exception ex)
            {
                return ApiExceptionHelper.HandleException(ex, _logger);
            }
        }
    }
}
