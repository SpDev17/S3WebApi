using Microsoft.AspNetCore.Mvc;
using S3WebApi.APIRoutes;
using S3WebApi.Helpers;
using S3WebApi.Interfaces;
using S3WebApi.Models;
using S3WebApi.Models.Common;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace S3WebApi.Controllers
{
    [ApiController]
    [Route(ApiRoutes.ControllerRoute.Controller)]
    public class PostgresController(IPostgresService postgresService, 
    ISharePointService sharePointService, IObjectStorageService objectStorageService,
    IWebHostEnvironment env, IConfiguration configuration) : ControllerBase
    {
        private readonly IPostgresService _postgresService = postgresService;
        private readonly IObjectStorageService _objectStorageService = objectStorageService;
        private Serilog.ILogger _logger => Serilog.Log.ForContext<PostgresController>();
        private readonly ISharePointService _sharePointService = sharePointService;

        [HttpPost("GetMetadata")]
        public async Task<IActionResult> GetMetadataDynamicAsync([FromBody] SearchCondition conditions)
        {
            try
            {
                string res_Site_URL = await _postgresService.GetSiteURL(conditions.Client_ID);
                if (string.IsNullOrEmpty(res_Site_URL.Trim()))
                {
                    _logger.AddMethodName().Warning("Bad request: Site url not available for this company!");
                    return ApiResponseHelper.NotFound("No documents found for the entered company number. Please check and try again!");
                }
                var site_Split_Val = res_Site_URL.Split("/");
                _logger.AddMethodName().Information("GetSPGroupList called");
                string siteUrl = site_Split_Val[0] + "//" + site_Split_Val[2] + "/" + site_Split_Val[3] + "/" + site_Split_Val[4] + "/" + conditions.Client_ID;
                List<SPGroupResponse> resultPermission = resultPermission = await _sharePointService.GetSPGroupList(siteUrl, conditions.Email_ID);

                if (resultPermission is not null)
                {
                    if (resultPermission.Count == 0)
                    {
                        _logger.AddMethodName().Information("You do not have sufficient permission, Please contact administrator");
                        return ApiResponseHelper.BadRequest("You do not have sufficient permission, Please contact administrator.");
                    }
                    var permissionCommaSeparated = string.Join(", ", resultPermission.Select(g => $"'Group: {g.Title}'"));
                    MetadataResult result = await _postgresService.GetMedatadataDynamicAsync(conditions, permissionCommaSeparated);

                    try
                    {
                        if (result is not null)
                        {
                            _logger.AddMethodName().Information("Metadata dynamic query returned {Count} results.", result?.TotalCount ?? 0);
                            return ApiResponseHelper.Ok(result, (result?.TotalCount == 0 ? "No record found!" : ""));
                        }
                        else
                        {
                            _logger.AddMethodName().Information("Metadata dynamic query returned no records.");
                            return ApiResponseHelper.Ok(result,"No records found!");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.AddMethodName().Error("Failed to parse JSON array string to MShareArchive exception : {0}", ex);
                        return ApiResponseHelper.BadRequest("An error occurred while processing your request.");
                    }
                }
                else
                {
                    _logger.AddMethodName().Error("Failed to get user permission for the user id {0}", conditions.Email_ID);
                    return ApiResponseHelper.BadRequest("User permission not available!");
                }
            }
            catch (Exception ex)
            {
                return ApiExceptionHelper.HandleException(ex, _logger);
            }
        }

        [HttpPost("ProcessArchiveQueue")]
        public async Task<ActionResult<object>> ProcessArchiveQueue([FromBody] ArchiveJobRequest request)
        {
            _logger.AddMethodName().Information("Calling _postgresService.GetArchiveQueueAsync function with param - jobId: {0} , limit : {1}", request.ruleId , request.limit);
            var queuedata = await _postgresService.GetArchiveQueueAsync(request.ruleId, request.limit);
            _logger.AddMethodName().Information("Fetched archive queue with status new successfully. Count: {Count}", queuedata?.Count() ?? 0);
            var apiResponses = new List<object>();
            int totalQueueItems = 0;
            if (queuedata != null && queuedata.Any())
            {
                string ApiUrl = configuration["ArchiveApiEndPoint"] + "ArchiveObject";

                string BearerToken = null;
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(300) };
                if (!string.IsNullOrEmpty(BearerToken))
                {
                    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
                }
                var requestdata = new S3FileRequest();
                requestdata.Country = request.Country;
                requestdata.LimitVersion = request.LimitVersion;
                requestdata.DeleteSource = request.DeleteSource;
                var filePathUrls = new List<string>();
                foreach (var item in queuedata)
                {
                    var dict = (IDictionary<string, object>)item;
                    if (dict.TryGetValue("FullPath", out object FullPath))
                    {
                        filePathUrls.Add(Convert.ToString(FullPath));
                    }
                    requestdata.Urls = filePathUrls;

                }
                var json = JsonSerializer.Serialize(requestdata);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                try
                {
                    var resp = await http.PostAsync(ApiUrl, content);
                    var respBody = await resp.Content.ReadAsStringAsync();
                    var responseEntry = new
                    {
                        //Item = item,
                        ApiCall_IsSuccess = resp.IsSuccessStatusCode,
                        ApiCall_StatusCode = resp.StatusCode,
                        ResponseBody = respBody
                    };
                    apiResponses.Add(responseEntry);
                    if (resp.IsSuccessStatusCode)
                    {
                        _logger.AddMethodName().Information("API response: {ResponseBody}", respBody);
                    }
                    else
                    {
                        _logger.AddMethodName().Warning("API error. Status: {StatusCode}. Body: {ResponseBody}", resp.StatusCode, respBody);
                    }
                }
                catch (Exception ex)
                {
                    var errorEntry = new
                    {
                        //Item = item,
                        ApiCall_IsSuccess = false,
                        ApiCall_StatusCode = 400,
                        ErrorMessage = ex.Message
                    };
                    apiResponses.Add(errorEntry);
                    _logger.AddMethodName().Error(ex, "HTTP error during ProcessArchiveQueue Archive API call.");
                }

                _logger.AddMethodName().Information("Calling _postgresService.GetArchiveQueueCountAsync function with param - jobId:" + request.ruleId);
                totalQueueItems = await _postgresService.GetArchiveQueueCountAsync(request.ruleId);
                _logger.AddMethodName().Information("DB response: - Total items to processed: {0}", totalQueueItems);
            }

            // Return both the original queuedata and the list of API responses
            return Ok(new
            {
                ApiResponses = apiResponses,
                Next = totalQueueItems
            });
        }

        [HttpPost("AutoFillArchiveQueue")]
        public async Task<ActionResult<object>> AutoFillArchiveQueue([FromBody] AutoFillArchiveQueueRequest request)
        {
            _logger.AddMethodName().Information("Entered AutoFillArchiveQueue method.");
            TableRequest table = new TableRequest();
            bool isPageno = int.TryParse(Convert.ToString(request.pageno), out int pg);
            if (!isPageno)
            {
                request.pageno = 1;
            }
            table.pageno = Convert.ToString(request.pageno);
            bool isLimit = int.TryParse(Convert.ToString(request.limit), out int lmt);
            if (!isLimit)
            {
                request.limit = 10;
            }
            table.limit = Convert.ToString(request.limit);
            if (!string.IsNullOrEmpty(request.ruleid))
            {
                table.where = " \"RuleID\"='" + request.ruleid + "' ";
            }

            table.tablename = "document_details";
            table.selectcolumn = "*";
            table.orderby = "id";
            table.sortorder = "asc";

            //table.pageno = Convert.ToString(request.pageno);
            var queuedata = await _postgresService.GetDataAsync(table);
            if (queuedata != null && queuedata.Any())
            {
                _logger.AddMethodName().Information("AutoFillArchiveQueue - Total rows read from db : {0}", queuedata.Count());
                bool IsSuccess = await _postgresService.BulkUpsert_Documents_To_ArchiveQueue(queuedata);
                if (IsSuccess)
                {
                    table.tablename = "document_details";
                    table.limit = string.Empty;
                    table.selectcolumn = "count(*)";
                    table.orderby = string.Empty;
                    table.sortorder = string.Empty;
                    table.pageno = string.Empty;
                    if (!string.IsNullOrEmpty(request.ruleid))
                    {
                        table.where = " \"RuleID\"='" + request.ruleid + "' ";
                    }
                    var countData = await _postgresService.GetDataAsync(table);
                    int totalRows = 0;

                    if (countData != null && countData.Any())
                    {
                        var dict = (IDictionary<string, object>)countData.First();
                        if (dict.TryGetValue("count", out object countValue))
                        {
                            totalRows = Convert.ToInt32(countValue);
                            _logger.AddMethodName().Information("Total rows db : {0}", totalRows);
                        }
                    }

                    int currentPage = request.pageno > 0 ? request.pageno : 1;
                    int pageSize = request.limit > 0 ? request.limit : 10;

                    int totalPages = (int)Math.Ceiling((double)totalRows / pageSize);

                    int nextPgNo = (currentPage < totalPages) ? currentPage + 1 : 0;
                    int prevPgNo = (currentPage > 1) ? currentPage - 1 : 0;
                    return Ok(new
                    {
                        data = queuedata.Count(),
                        nextPage = nextPgNo,
                        Error = string.Empty
                    });
                }
                else
                {
                    return Ok(new
                    {
                        data = string.Empty,
                        nextPage = 0,
                        Error = "Bulk upsert is failed. Kindly check log for details."
                    });
                }

            }
            else
            {
                return Ok(new
                {
                    data = string.Empty,
                    nextPage = 0,
                    Error = "No data present."
                });
            }
        }

        [HttpPost("InsertSearchDocuments")]
        public async Task<ActionResult<object>> InsertSearchDocuments([FromBody] List<DocumentDetails_Item> items, CancellationToken ct)
        {
            if (items == null || items.Count == 0)
                return BadRequest(new { Message = "No items provided", ReturnCode = 400 });
                
            await _postgresService.BulkUpsertDocumentDetailsAsync(items, ct);
            return Ok(new
            {
                ReturnCode = 200,
                Message = string.Empty,
            });
        }

        [HttpGet("GetDocVersionsById/{id}")]
        public async Task<IActionResult> GetDocumentVersionsById([Required] string id)
        {            
            if (!Guid.TryParse(id, out Guid validGuid))
            {
                _logger.AddMethodName().Warning("Bad request: Id : {0} is incorrect!", id);
                return ApiResponseHelper.BadRequest("Id is incorrect!");
            }
            try
            {
                MetadataResult result = await _postgresService.GetDocumentVersionsIdAsync(id);
                if (result is not null)
                {
                    _logger.AddMethodName().Information("Document versions dynamic query returned {Count} results.", result?.TotalCount ?? 0);
                    return ApiResponseHelper.Ok(result, (result?.TotalCount == 0 ? "No record found!" : ""));
                }
                else
                {
                    _logger.AddMethodName().Information("Document versions dynamic query returned no records.");
                    return ApiResponseHelper.Ok(result, "No records found!");
                }
            }
            catch (Exception ex)
            {
                return ApiExceptionHelper.HandleException(ex, _logger);
            }
        }

        [HttpPost("downloadfile")]
        public async Task<IActionResult> DownloadFileStreamAsync([FromBody] FileDownloadRequest request)
        {
            _logger.AddMethodName().Information("DownloadFileStreamAsync method called with request: {@Request}", request);
            
            try
            {
                string res = await _postgresService.ValidateFileBeforeDownload(request);
                if (!string.IsNullOrEmpty(res))
                {
                    _logger.AddMethodName().Warning("ValidateFileBeforeDownload failed: {0}", res);
                    return ApiResponseHelper.BadRequest(res);
                }

                string siteUrl = request.SiteURL + request.Client_ID;
                // string siteUrl = "https://devmmcglobal.sharepoint.com/sites/MRSH-MShareDev-INST-GB001/" + request.ClientID;
                List<SPGroupResponse> resultPermission = await _sharePointService.GetSPGroupList(siteUrl, request.Email_ID);

                if (resultPermission is not null && resultPermission?.Count > 0)
                {
                    _logger.AddMethodName().Information("DownloadFileStreamAsync permission granted for EmailID: {EmailID}", request.Email_ID);

                    var result = await _objectStorageService.DownloadStreamAsync(request, resultPermission);

                    if (result != null)
                    {
                        _logger.AddMethodName().Information("DownloadFileStreamAsync succeeded for FileUrl: {FileUrl}", request.FileUrl);
                    }
                    else
                    {
                        _logger.AddMethodName().Warning("DownloadFileStreamAsync failed: File not found for FileUrl: {FileUrl}", request.FileUrl);
                    }
                    return (IActionResult?)result ??  ApiResponseHelper.NotFound("File not found!");
                }
                else
                {
                    _logger.AddMethodName().Warning("DownloadFileStreamAsync permission denied for EmailID: {EmailID}", request.Email_ID);
                    return ApiResponseHelper.Unauthorized("You don't have permission to download!");
                }
            }
            catch (Exception ex)
            {
                return ApiExceptionHelper.HandleException(ex, _logger);
            }
        }
    }
}
