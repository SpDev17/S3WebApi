using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Graph.Models;
using Microsoft.Graph;
using System.Text.Json;
using S3WebApi.Types;
using System.Text;
using Validation;
using S3WebApi.Models;
using PnP.Framework.Extensions;
using Amazon;
using S3WebApi.DMSAuth;
using S3WebApi.Interfaces;
using ListItem = Microsoft.Graph.Models.ListItem;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Net;
using Constants = S3WebApi.GlobalLayer.Constants;
using S3WebApi.Helpers;
using Amazon.S3.Model;
using S3WebApi.Models.Docs;

namespace S3WebApi.Repository
{
    public class SharePointRepository : ISharePointRepository
    {
        private const string ResourceNotFound = "The resource could not be found.";
        private readonly IConfiguration _configuration;
        private readonly GraphClient _graphClient;
        private readonly ISiteRepository _siteRepository;
        private readonly IPostgresRepository _postgresRepository;
        private readonly IS3Repository _s3Repository;
        private readonly AssumeRoleRequestDto _settings;
        private readonly AuthSecret _authSecret;
        private readonly IDMSAuthOperation _dMSAuthOperation;
        private readonly IDMSAuthOperation _authenticationProvider;
        private Serilog.ILogger _logger => Serilog.Log.ForContext<SharePointRepository>();
        private static readonly HttpClient Http = new HttpClient() { Timeout = TimeSpan.FromSeconds(100) };
        private static readonly Regex AadGuidRegex = new Regex(@"([0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12})$", RegexOptions.Compiled);
        private readonly int _retryCount;
        private readonly int _delay;
        private readonly string _viewAliasDocumentUrl;

        public SharePointRepository(
            ISiteRepository siteRepository,
            GraphClient graphClient,
            IConfiguration configuration,
            AuthSecret authSecret,
            IPostgresRepository postgresRepository,
            IS3Repository s3Repository,
            IDMSAuthOperation dMSAuthOperation, IDMSAuthOperation authenticationProvider)
        {
            _siteRepository = siteRepository ?? throw new ArgumentNullException(nameof(siteRepository));
            _graphClient = graphClient ?? throw new ArgumentNullException(nameof(graphClient));
            _s3Repository = s3Repository;
            _postgresRepository = postgresRepository;
            _settings = configuration.GetSection("AwsS3PVCSettings").Get<AssumeRoleRequestDto>();
            _authSecret = authSecret;
            var region = RegionEndpoint.GetBySystemName(_settings.Region);
            _dMSAuthOperation = dMSAuthOperation;
            _authenticationProvider = authenticationProvider;
            _retryCount = Convert.ToInt32(configuration["DynamicConfigurations:RetryCount"]);
            _delay = Convert.ToInt32(configuration["DynamicConfigurations:Delay"]);
            _viewAliasDocumentUrl = configuration[Constants.ViewAliasDocumentUrl].ToString();
            _configuration = configuration;
        }

        public async Task<List<MShareArchive>> GetFileMetaInfoAsync<T>(
            string contextUrl,
            InstitutionLibrary library,
            string itemPath, string location, string item, string client, string country, bool LimitVersion, string contentType) where T : class, IDocumentModel
        {
            _logger.Information("Entered GetFileAsync with contextUrl: {ContextUrl}, itemPath: {ItemPath}", contextUrl, itemPath);
            Requires.NotNullOrWhiteSpace(contextUrl, nameof(contextUrl));
            Requires.NotNullOrWhiteSpace(itemPath, nameof(itemPath));

            var siteId = await _siteRepository.GetSiteIdAsync(contextUrl);
            var libraryId = await _siteRepository.GetLibraryAsync(contextUrl, siteId, library);

            location = location.Trim('/');
            ListItem? listItemResult;

            try
            {
                ListCollectionResponse getList1 = await _graphClient.SpoInstance(contextUrl).
                    Sites[siteId]
                    .Lists
                    .GetAsync();

                var itm = await _graphClient.SpoInstance(contextUrl)
                .Drives[libraryId]
                .Root
                .ItemWithPath(item)
                .ListItem
                .GetAsync();

                var lst = getList1.Value
                    .Where(x => x.DisplayName == location)
                    .Select(x => x.Id)
                    .FirstOrDefault();

                var listitd = lst;
                var itemid = itm.Id;

                ListItemVersionCollectionResponse listItem = null;
                List<MShareArchive> mDataList =  new List<MShareArchive>();
                if(lst != null && LimitVersion == true)
                {
                    listItem = await _graphClient.SpoInstance(contextUrl).Sites[siteId]
                    .Lists[listitd]
                    .Items[itemid]
                    .Versions
                    .GetAsync();

                    foreach (var ItemResult in listItem.Value)
                    {
                        string metadataJson = string.Empty;

                        // Handle raw object-to-JSON safely
                        var dictionary = new Dictionary<string, object>();

                        var documentInfo1 =
                                new DocumentInfo(
                                    itemPath!,
                                    libraryId,
                                    itemPath,
                                    contextUrl,
                                    itemPath!,
                                    ItemResult.Id!,
                                    Utility.GetLibraryName(library));

                        var metaObject = ListItemBinder.Bind<T>(
                                    ItemResult.Fields!.AdditionalData,
                                    documentInfo1);

                        var properties = metaObject.GetType().GetProperties();
                        var propertyValues = properties.Select(p => dictionary[p.Name] = p.GetValue(metaObject)).ToList();


                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };
                        dictionary["ContentType"] = contentType;

                        // Object Metadata to Json
                        metadataJson = JsonSerializer.Serialize(dictionary, options);

                        var permissions = await _graphClient.SpoInstance(contextUrl)
                                    .Drives[libraryId]
                                    .Root
                                    .ItemWithPath(item)
                                    .Permissions
                                    .GetAsync();

                        List<PermissionsList> permissionList = new List<PermissionsList>();
                        if (permissions?.Value == null || permissions.Value.Count == 0)
                        {
                            _logger.Information("No permissions found for this file.");
                        }
                        else
                        {
                            foreach (var perm in permissions.Value)
                            {
                                string who = "";
                                string role = perm.Roles?.FirstOrDefault() ?? "Unknown";
                                string sharedByLink = perm.Link?.Scope ?? "Direct access";

                                if (perm.GrantedToV2?.User != null)
                                {
                                    who = $"User: {perm.GrantedToV2.User.DisplayName} ({perm.GrantedToV2.User.Id})";
                                }
                                else if (perm.GrantedToV2?.SiteGroup != null)
                                {
                                    who = $"Group: {perm.GrantedToV2.SiteGroup.DisplayName}";
                                }
                                else if (perm.GrantedToV2?.SiteUser != null)
                                {
                                    who = $"Group: {perm.GrantedToV2.SiteUser.DisplayName}";
                                }
                                else if (perm.Link != null)
                                {
                                    who = $"Shared Link ({perm.Link.Scope})";
                                }
                                else
                                {
                                    who = "Unknown recipient";
                                }

                                permissionList.Add(new PermissionsList(who, role, sharedByLink));
                            }
                        }

                        MShareArchive mData = new MShareArchive();
                        mData.ObjectID = metaObject.ObjectId;
                        mData.ClientID = client;
                        mData.ContentType = contentType;
                        mData.DocumentTypeMetadata = metadataJson;
                        mData.ExtendedMetadata = metaObject.GetPublicInstancePropertyValue("ExtendedMetadata")?.ToString();
                        mData.DateOfArchival = DateTime.Now.ToString();
                        mData.SecurityInfo = JsonSerializer.Serialize(permissionList, options);
                        mData.DocumentArchiveLocation = "https://" + _settings.BucketName + ".S3." + _settings.Region + ".amazonaws.com/" + country + "/" + client + "/" + location + "/" + item;
                        mData.StorageLocation = itemPath;
                        mData.Country = country;
                        mData.BucketName = _settings.BucketName;
                        mData.FileName = Path.GetFileName(item);
                        mData.LibraryName = location;
                        mData.DocCreatedDate = metaObject.GetPublicInstancePropertyValue("CreatedDateUtc").ToString();
                        mData.DocModifiedDate = metaObject.GetPublicInstancePropertyValue("ModifiedDateUtc").ToString();
                        mDataList.Add(mData);
                    }
                    return mDataList;
                }
                else
                {
                    string metadataJson = string.Empty;

                    // Handle raw object-to-JSON safely
                    var dictionary = new Dictionary<string, object>();

                    var documentInfo1 =
                            new DocumentInfo(
                                itemPath!,
                                libraryId,
                                itemPath,
                                contextUrl,
                                itemPath!,
                                itm.Id!,
                                Utility.GetLibraryName(library));

                    var metaObject = ListItemBinder.Bind<T>(
                                itm.Fields!.AdditionalData,
                                documentInfo1);

                    var properties = metaObject.GetType().GetProperties();
                    var propertyValues = properties.Select(p => dictionary[p.Name] = p.GetValue(metaObject)).ToList();


                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    dictionary["ContentType"] = contentType;

                    // Object Metadata to Json
                    metadataJson = JsonSerializer.Serialize(dictionary, options);

                    var permissions = await _graphClient.SpoInstance(contextUrl)
                                .Drives[libraryId]
                                .Root
                                .ItemWithPath(item)
                                .Permissions
                                .GetAsync();

                    List<PermissionsList> permissionList = new List<PermissionsList>();
                    if (permissions?.Value == null || permissions.Value.Count == 0)
                    {
                        _logger.Information("No permissions found for this file.");
                    }
                    else
                    {
                        foreach (var perm in permissions.Value)
                        {
                            string who = "";
                            string role = perm.Roles?.FirstOrDefault() ?? "Unknown";
                            string sharedByLink = perm.Link?.Scope ?? "Direct access";

                            if (perm.GrantedToV2?.User != null)
                            {
                                who = $"User: {perm.GrantedToV2.User.DisplayName} ({perm.GrantedToV2.User.Id})";
                            }
                            else if (perm.GrantedToV2?.SiteGroup != null)
                            {
                                who = $"Group: {perm.GrantedToV2.SiteGroup.DisplayName}";
                            }
                            else if (perm.GrantedToV2?.SiteUser != null)
                            {
                                who = $"Group: {perm.GrantedToV2.SiteUser.DisplayName}";
                            }
                            else if (perm.Link != null)
                            {
                                who = $"Shared Link ({perm.Link.Scope})";
                            }
                            else
                            {
                                who = "Unknown recipient";
                            }

                            permissionList.Add(new PermissionsList(who, role, sharedByLink));
                        }
                    }

                    MShareArchive mData = new MShareArchive(); 
                    mData.ObjectID = metaObject.GetPublicInstancePropertyValue("ObjectId").ToString();
                    mData.ClientID = client;
                    mData.ContentType = metaObject.GetPublicInstancePropertyValue("ContentType").ToString();
                    mData.DocumentTypeMetadata = metadataJson;
                    mData.ExtendedMetadata = metaObject.GetPublicInstancePropertyValue("ExtendedMetadata")?.ToString();
                    mData.DateOfArchival = DateTime.Now.ToString();
                    mData.SecurityInfo = JsonSerializer.Serialize(permissionList, options);
                    mData.DocumentArchiveLocation = "https://" + _settings.BucketName + ".S3." + _settings.Region + ".amazonaws.com/" + country + "/" + client + "/" + location + "/" + item;
                    mData.StorageLocation = itemPath;
                    mData.Country = country;
                    mData.BucketName = _settings.BucketName;
                    mData.FileName = Path.GetFileName(item);
                    mData.LibraryName = location;
                    mData.DocCreatedDate = metaObject.GetPublicInstancePropertyValue("CreatedDateUtc").ToString();
                    mData.DocModifiedDate = metaObject.GetPublicInstancePropertyValue("ModifiedDateUtc").ToString();
                    mDataList.Add(mData);
                }                 
                return mDataList;
            }
            catch (ODataError error)
            {
                _logger.Error(error, "Error in GetFileAsync with itemPath: {ItemPath}", itemPath);
                await _postgresRepository.UpdateArchiveQueueStatusAsync("Error in GetFileAsync() method ---" + error.Message, itemPath, null);
                if (error.Message == ResourceNotFound)
                {
                    _logger.Warning("Resource not found: {Message}", error.Message);
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in GetFileAsync with itemPath: {ItemPath}", itemPath);
                throw;
            }
        }

        public async Task<(Stream FileStream, String FileName)> DownloadFileByUrlAsync(string fileUrl, string versionId, bool latest)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL must be provided.");

            dynamic driveItem;
            dynamic stream;
            // Encode and convert full URL to a Graph share ID (u!Base64-encoded)
            string base64Url = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileUrl.TrimEnd())).Replace("/", "_").Replace("+", "-").TrimEnd('=');
            try
            {
                driveItem = await _graphClient.SpoInstance(fileUrl.TrimEnd())
                    .Shares[$"u!{base64Url}"]
                    .DriveItem
                    .GetAsync();

                if (driveItem == null || string.IsNullOrEmpty(driveItem.Id))
                    throw new FileNotFoundException("Unable to locate file from provided URL.");
                if (latest)
                {
                    stream = await _graphClient.SpoInstance(fileUrl)
                        .Drives[driveItem.ParentReference.DriveId]
                        .Items[driveItem.Id]
                        .Content
                        .GetAsync();
                }
                else
                {
                    stream = await _graphClient.SpoInstance(fileUrl)
                        .Drives[driveItem.ParentReference.DriveId]
                        .Items[driveItem.Id]
                        .Versions[versionId]
                        .Content
                        .GetAsync();
                }

                //await _postgresRepository.UpdateArchiveQueueStatusAsync("download stream is done", fileUrl);
                return (stream, driveItem.Name);

            }
            catch (Exception ex)
            {
                _logger.Error("Error in DownloadFileByUrlAsync() method ", ex.Message);
                _postgresRepository.UpdateArchiveQueueStatusAsync("Error in DownloadFileByUrlAsync() method ---" + ex.Message, fileUrl, null);
                throw;
            }

        }

        public async Task<DriveItemVersionCollectionResponse> GetFileVersions(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new ArgumentException("File URL must be provided.");

            dynamic driveItem;
            dynamic stream;

            // Encode and convert full URL to a Graph share ID (u!Base64-encoded)
            string base64Url = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileUrl.TrimEnd())).Replace("/", "_").Replace("+", "-").TrimEnd('=');
            try
            {
                driveItem = await _graphClient.SpoInstance(fileUrl.TrimEnd())
                .Shares[$"u!{base64Url}"]
                .DriveItem
                .GetAsync();


                if (driveItem == null || string.IsNullOrEmpty(driveItem.Id))
                    throw new FileNotFoundException("Unable to locate file from provided URL.");

                var versions = await _graphClient.SpoInstance(fileUrl)
                    .Drives[driveItem.ParentReference.DriveId]
                    .Items[driveItem.Id]
                    .Versions
                    .GetAsync();

                return versions;

            }
            catch (Exception ex)
            {
                _logger.Error("Error in GetFileVersions() method ", ex.Message);
                _postgresRepository.UpdateArchiveQueueStatusAsync("Error in GetFileVersions() method ---- " + ex.Message, fileUrl, null);
                throw;
            }
        }


        public async Task<string> GetContentType<T>(string fileUrl, InstitutionLibrary library, string item, string contextUrl) where T : class, IDocumentModel
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
                throw new HttpStatusCodeException(400, "File URL must be provided for get content type.");

            dynamic driveItem;
            dynamic stream;

            // Encode and convert full URL to a Graph share ID (u!Base64-encoded)
            string base64Url = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileUrl.TrimEnd())).Replace("/", "_").Replace("+", "-").TrimEnd('=');
            try
            {
                var siteId = await _siteRepository.GetSiteIdAsync(contextUrl);
                var libraryId = await _siteRepository.GetLibraryAsync(contextUrl, siteId, library);

                var itm = await _graphClient.SpoInstance(contextUrl)
                .Drives[libraryId]
                .Root
                .ItemWithPath(item)
                .ListItem
                .GetAsync();

                string metadataJson = string.Empty;

                // Handle raw object-to-JSON safely
                var dictionary = new Dictionary<string, object>();

                var documentInfo1 =
                        new DocumentInfo(
                            fileUrl!,
                            libraryId,
                            fileUrl,
                            contextUrl,
                            fileUrl!,
                            itm.Id!,
                            Utility.GetLibraryName(library));

                var metaObject = ListItemBinder.Bind<T>(
                            itm.Fields!.AdditionalData,
                            documentInfo1);

                return metaObject.GetPublicInstancePropertyValue("ContentType").ToString();

            }
            catch (Exception ex)
            {
                _logger.Error("Error in GetContentType() method ", ex.Message);
                _postgresRepository.UpdateArchiveQueueStatusAsync("Error in GetContentType() method ---" + ex.Message, fileUrl, "");
                throw new HttpStatusCodeException(400, ex.Message);
            }
        }

        public async Task<List<SPGroupResponse>> GetSPGroupList(string SiteUrl, string Email)
        {
            if (string.IsNullOrWhiteSpace(SiteUrl))
                throw new ArgumentException("Site URL must be provided.");
            if (string.IsNullOrWhiteSpace(Email))
                throw new ArgumentException("Email must be provided.");
            List<string> spGroupList = new List<string>();
            List<SPGroupResponse> sharePointGroupInfosList = new List<SPGroupResponse>();
            try
            {
                string accessTokenSPO = _authenticationProvider.AcquireTokenWithCertificateAsync(null, _configuration["Tenant_Url"]).GetAwaiter().GetResult();

                string accessTokenGraph = _authenticationProvider.AcquireTokenWithCertificateAsync(null, null).GetAwaiter().GetResult();

                var userInfo = await GetUserInfoByIdAsync(accessTokenGraph, Email);
                if (!string.IsNullOrEmpty(userInfo.userPrincipalName))
                {
                    _logger.Information($"Target user: id={userInfo.userId}, upn={userInfo.userPrincipalName}, displayName={userInfo.displayName}");

                    var roleAssignmentPrincipals = await EnumerateRoleAssignmentPrincipalsAsync(SiteUrl, accessTokenSPO);
                    
                    var siteGroups = await EnumerateSiteGroupsAsync(SiteUrl, accessTokenSPO);
                    
                    // Build map: SP group id -> group info
                    var spGroupMap = siteGroups.ToDictionary(g => g.GroupId, g => g);

                    // 3) Collect candidate AAD group ids found in principals (role assignments + SP group members)
                    var candidateAadGroupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _logger.Information(" ");

                    _logger.Information("------------------- printing  candidateAadGroupIds --------------------");

                    // Helper to parse and add AAD GUID from a principal's LoginName
                    void TryAddAadGuidFromLogin(string login)
                    {
                        if (string.IsNullOrEmpty(login)) return;
                        var m = AadGuidRegex.Match(login);
                        if (m.Success)
                        {
                            candidateAadGroupIds.Add(m.Groups[1].Value);
                            //_logger.Information(m.Groups[1].Value);
                        }
                    }

                    // From role assignment principals
                    foreach (var p in roleAssignmentPrincipals)
                    {
                        TryAddAadGuidFromLogin(p.LoginName);
                    }

                    // From site groups' members
                    foreach (var grp in siteGroups)
                    {
                        foreach (var m in grp.Members)
                            TryAddAadGuidFromLogin(m.LoginName);
                    }

                    _logger.Information($"Collected {candidateAadGroupIds.Count} unique candidate AAD group ids referenced by site principals.");
                    _logger.Information("------------------------Get User by email-----------------------");

                    var directMembershipGroups = new HashSet<int>(); // SP group ids where user is direct member
                    bool directInRoleAssignments = false;
                    // Check role assignment principals directly
                    foreach (var p in roleAssignmentPrincipals)
                    {
                        if ((p.PrincipalType & 1) != 0) // user principal
                        {
                            if (!string.IsNullOrEmpty(p.LoginName) && p.LoginName.IndexOf(userInfo.userPrincipalName, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                directInRoleAssignments = true;
                            }
                        }
                    }

                    // Check membership via site group expansion
                    foreach (var grp in siteGroups)
                    {
                        foreach (var m in grp.Members)
                        {
                            if ((m.PrincipalType & 1) != 0) // user principal
                            {
                                if (!string.IsNullOrEmpty(m.LoginName) && m.LoginName.IndexOf(userInfo.userPrincipalName, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    directMembershipGroups.Add(grp.GroupId);
                                    break;
                                }
                            }
                        }
                    }

                    // 6) Evaluate membership via AAD groups: use POST /users/{id}/checkMemberGroups (chunked to 20)
                    _logger.Information("----------------Get Transitive Member-----------------");
                    var matchedAadGroupIds = await GetUser_TransitiveMemberOfGrp(userInfo.userId, accessTokenGraph);
                    var spGroupsViaAad = new HashSet<int>(); // SP group ids where membership arises via AAD group
                                                             // find SP groups that include a principal with that AAD GUID or where role assignment principal is that AAD GUID
                    foreach (var grp in siteGroups)
                    {
                        foreach (var m in grp.Members)
                        {
                            var mm = AadGuidRegex.Match(m.LoginName ?? "");
                            if (mm.Success && matchedAadGroupIds.Contains(mm.Groups[1].Value))
                            {
                                spGroupsViaAad.Add(grp.GroupId);
                                break;
                            }
                        }
                    }

                    // Also consider role assignment principals that are AAD groups directly (not SP groups)
                    var roleAssignmentAadGroupMatched = new List<PrincipalInfo>();
                    foreach (var p in roleAssignmentPrincipals)
                    {
                        var mm = AadGuidRegex.Match(p.LoginName ?? "");
                        if (mm.Success && matchedAadGroupIds.Contains(mm.Groups[1].Value))
                        {
                            roleAssignmentAadGroupMatched.Add(p);
                        }
                    }
                    // 8) Compose final effective groups / role assignments representing user
                    var effectiveGroups = new List<(int groupId, string title, string via)>(); // via: Direct / AADGroup / RoleAssignmentDirect

                    // Add direct SP group membership
                    foreach (var gid in directMembershipGroups)
                    {
                        var g = spGroupMap.GetValueOrDefault(gid);
                        effectiveGroups.Add((gid, g?.Title ?? $"SPGroup[{gid}]", "DirectUser"));
                        sharePointGroupInfosList.Add(new SPGroupResponse() { GroupId = gid, Title = g?.Title });
                    }

                    // Add SP groups via AAD groups
                    foreach (var gid in spGroupsViaAad)
                    {
                        var g = spGroupMap.GetValueOrDefault(gid);
                        // avoid duplicate
                        if (!effectiveGroups.Any(e => e.groupId == gid))
                        {
                            effectiveGroups.Add((gid, g?.Title ?? $"SPGroup[{gid}]", "ViaAadGroup"));
                            sharePointGroupInfosList.Add(new SPGroupResponse() { GroupId = gid, Title = g?.Title });
                        }
                    }

                    // Add role assignments that are AAD groups (principal directly assigned to site/list)
                    foreach (var rp in roleAssignmentAadGroupMatched)
                    {
                        // If the role assignment member is not a SP group but an AAD group assigned directly, we record it here.
                        string title = rp.Title ?? rp.LoginName;
                        effectiveGroups.Add((groupId: -1, title: $"RoleAssignmentPrincipal: {title}", via: "RoleAssignmentAadGroup"));
                        sharePointGroupInfosList.Add(new SPGroupResponse() { GroupId = -1, Title = title });
                    }

                    // Add if user was a user principal directly on role assignments (not within a SP group)
                    if (directInRoleAssignments)
                    {
                        effectiveGroups.Add((groupId: -1, title: "Direct role assignment for user principal", via: "DirectUserRoleAssignment"));
                        sharePointGroupInfosList.Add(new SPGroupResponse() { GroupId = -1, Title = "Direct role assignment for user principal" });
                    }

                    // 9) Output results
                    _logger.Information("=== Effective SharePoint groups / role assignments representing the user ===");
                    foreach (var eg in effectiveGroups)
                    {
                        _logger.Information($"{eg.groupId,-6} | {eg.title} | via={eg.via}");
                    }
                    if (effectiveGroups.Count == 0)
                        _logger.Information("No matching SharePoint groups / role assignments found for the user.");
                }
                else
                {
                    _logger.Information("Specified email id is not present - " + Email);
                    throw new Exception("Specified email id is not present - " + Email);
                }

            }
            catch (Exception ex)
            {
                _logger.Error("Error in GetSPGroupList() method ", ex.Message);
                _postgresRepository.UpdateArchiveQueueStatusAsync("Error in GetSPGroupList() method ---" + ex.Message, SiteUrl, "");
                throw;
            }
            return sharePointGroupInfosList;
        }

        private async Task<List<PrincipalInfo>> EnumerateRoleAssignmentPrincipalsAsync(string siteUrl, string spoAccessToken)
        {
            var principals = new List<PrincipalInfo>();
            string requestUrl = $"{siteUrl.TrimEnd('/')}/_api/web/roleassignments?$expand=Member,RoleDefinitionBindings";

            while (!string.IsNullOrEmpty(requestUrl))
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", spoAccessToken);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var res = await SendWithRetryAsync(() => Http.SendAsync(req));
                if (!res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"SharePoint roleassignments request failed {res.StatusCode}: {body}");
                }

                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                // REST responses may vary; with nometadata we expect "value"
                JsonElement array;
                if (root.TryGetProperty("value", out array))
                {
                    // ok
                }
                else if (root.TryGetProperty("d", out var d) && d.TryGetProperty("results", out var r))
                {
                    array = r;
                }
                else
                {
                    throw new Exception("Unexpected SharePoint response shape for roleassignments.");
                }
                _logger.Information(" ");
                _logger.Information("-----------------Role Assignment grp---------------------");
                foreach (var item in array.EnumerateArray())
                {
                    // member is the principal
                    if (item.TryGetProperty("Member", out var member))
                    {
                        var p = new PrincipalInfo();
                        if (member.TryGetProperty("Id", out var idElem) && idElem.ValueKind == JsonValueKind.Number)
                            p.Id = idElem.GetInt32();
                        if (member.TryGetProperty("Title", out var titleElem) && titleElem.ValueKind == JsonValueKind.String)
                            p.Title = titleElem.GetString();
                        if (member.TryGetProperty("LoginName", out var lnElem) && lnElem.ValueKind == JsonValueKind.String)
                            p.LoginName = lnElem.GetString();
                        if (member.TryGetProperty("PrincipalType", out var ptElem) && ptElem.ValueKind == JsonValueKind.Number)
                            p.PrincipalType = ptElem.GetInt32();

                        p.IsSharePointGroup = (p.PrincipalType & 8) != 0; // SPGroup flag
                        //_logger.Information(p.Id + "," + p.Title + "," + p.LoginName + "," + p.PrincipalType);

                        principals.Add(p);
                    }
                }
                _logger.Information("-----------------It Ends---------------------");
                _logger.Information(" ");
                // next link handling
                string nextLink = null;
                if (root.TryGetProperty("@odata.nextLink", out var nl) && nl.ValueKind == JsonValueKind.String)
                    nextLink = nl.GetString();
                else if (root.TryGetProperty("d", out var d2) && d2.TryGetProperty("__next", out var next2) && next2.ValueKind == JsonValueKind.String)
                    nextLink = next2.GetString();

                requestUrl = nextLink;
            }

            return principals;
        }

        private static async Task<HttpResponseMessage> SendWithRetryAsync(Func<Task<HttpResponseMessage>> operation, int maxAttempts = 5)
        {
            int attempt = 0;
            var rnd = new Random();
            while (true)
            {
                attempt++;
                HttpResponseMessage resp = null;
                try
                {
                    resp = await operation().ConfigureAwait(false);
                }
                catch (HttpRequestException) when (attempt < maxAttempts)
                {
                    // transient network error
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))).ConfigureAwait(false);
                    continue;
                }

                if (resp == null) throw new Exception("Null response from HTTP operation");

                if ((int)resp.StatusCode == 429 || resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    if (attempt >= maxAttempts) return resp;

                    TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    if (resp.Headers.RetryAfter != null)
                    {
                        if (resp.Headers.RetryAfter.Delta.HasValue)
                            delay = resp.Headers.RetryAfter.Delta.Value;
                        else if (resp.Headers.RetryAfter.Date.HasValue)
                        {
                            var dt = resp.Headers.RetryAfter.Date.Value;
                            delay = dt - DateTimeOffset.UtcNow;
                            if (delay < TimeSpan.Zero) delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                        }
                    }

                    // jitter
                    delay += TimeSpan.FromMilliseconds(rnd.Next(0, 500));
                    resp.Dispose();
                    await Task.Delay(delay).ConfigureAwait(false);
                    continue;
                }

                // retry 5xx
                if ((int)resp.StatusCode >= 500 && attempt < maxAttempts)
                {
                    resp.Dispose();
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))).ConfigureAwait(false);
                    continue;
                }

                return resp;
            }
        }

        private async Task<List<SharePointGroupInfo>> EnumerateSiteGroupsAsync(string siteUrl, string spoAccessToken)
        {
            var results = new List<SharePointGroupInfo>();
            string requestUrl = $"{siteUrl.TrimEnd('/')}/_api/web/sitegroups?$expand=Users"; //($select=Id,Title,LoginName,PrincipalType)&$select=Id,Title";

            while (!string.IsNullOrEmpty(requestUrl))
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", spoAccessToken);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var res = await SendWithRetryAsync(() => Http.SendAsync(req));
                if (!res.IsSuccessStatusCode)
                {
                    var body = await res.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"SharePoint sitegroups request failed {res.StatusCode}: {body}");
                }

                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                JsonElement array;
                if (root.TryGetProperty("value", out array))
                {
                }
                else if (root.TryGetProperty("d", out var d) && d.TryGetProperty("results", out var r))
                {
                    array = r;
                }
                else
                {
                    throw new Exception("Unexpected SharePoint response shape for sitegroups.");
                }
                _logger.Information(" ");
                _logger.Information("-------------------------SiteGroup-----------------------------");
                foreach (var g in array.EnumerateArray())
                {
                    int id = g.GetProperty("Id").GetInt32();
                    string title = g.GetProperty("Title").GetString();

                    var grp = new SharePointGroupInfo { GroupId = id, Title = title, SiteUrl = siteUrl };

                    if (g.TryGetProperty("Users", out var usersElem) && usersElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var u in usersElem.EnumerateArray())
                        {
                            var p = new PrincipalInfo();
                            if (u.TryGetProperty("Id", out var idp) && idp.ValueKind == JsonValueKind.Number)
                                p.Id = idp.GetInt32();
                            if (u.TryGetProperty("Title", out var t) && t.ValueKind == JsonValueKind.String)
                                p.Title = t.GetString();
                            if (u.TryGetProperty("LoginName", out var ln) && ln.ValueKind == JsonValueKind.String)
                                p.LoginName = ln.GetString();
                            if (u.TryGetProperty("PrincipalType", out var pt) && pt.ValueKind == JsonValueKind.Number)
                                p.PrincipalType = pt.GetInt32();

                            grp.Members.Add(p);
                            //_logger.Information(p.Id + "," + p.Title + "," + p.LoginName + "," + p.PrincipalType);

                        }
                    }

                    results.Add(grp);
                }

                // next link
                string nextLink = null;
                if (root.TryGetProperty("@odata.nextLink", out var nl) && nl.ValueKind == JsonValueKind.String)
                    nextLink = nl.GetString();
                else if (root.TryGetProperty("d", out var d2) && d2.TryGetProperty("__next", out var next2) && next2.ValueKind == JsonValueKind.String)
                    nextLink = next2.GetString();

                requestUrl = nextLink;
            }

            return results;
        }

        private async Task<(string userId, string userPrincipalName, string displayName)> GetUserInfoByIdAsync(string graphAccessToken, string userObjectId)
        {
            try
            {
                var url = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(userObjectId)}?$select=id,displayName,userPrincipalName";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var res = await SendWithRetryAsync(() => Http.SendAsync(req));
                res.EnsureSuccessStatusCode();
                var json = await res.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string id = root.GetProperty("id").GetString();
                string upn = root.TryGetProperty("userPrincipalName", out var elUpn) ? elUpn.GetString() : null;
                string dn = root.TryGetProperty("displayName", out var elDn) ? elDn.GetString() : null;
                return (id, upn, dn);
            }
            catch (Exception ex)
            {
                _logger.Error("Error in GetUserInfoByIdAsync() method ", ex.Message);
                _postgresRepository.UpdateArchiveQueueStatusAsync("Error in GetUserInfoByIdAsync() method ---" + ex.Message, null, "");
                return (null, null, null);
            }
        }

        private async Task<HashSet<string>> GetUser_TransitiveMemberOfGrp(string email, string graphAccessToken)
        {
            try
            {
                var userGroupsIdList = new List<string>();
                var matchedAadGroupIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                string checkMemberGroupsEndpoint = $"https://graph.microsoft.com/beta/users/{Uri.EscapeDataString(email)}/transitiveMemberOf";
                using var req = new HttpRequestMessage(HttpMethod.Get, checkMemberGroupsEndpoint);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", graphAccessToken);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await SendWithRetryAsync(() => Http.SendAsync(req));
                response.EnsureSuccessStatusCode();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Warning($"Failed to check member groups. Status: {response.StatusCode}");
                    return null;
                }
                string jsonString = await response.Content.ReadAsStringAsync();

                // Parse JSON response
                using JsonDocument jsonDoc = JsonDocument.Parse(jsonString);
                var userGroupsDict = new Dictionary<string, string>();
                if (jsonDoc.RootElement.TryGetProperty("value", out JsonElement valueElement) && valueElement.ValueKind == JsonValueKind.Array)
                {
                    string groupId = string.Empty;
                    string displayName = string.Empty;

                    foreach (JsonElement groupElement in valueElement.EnumerateArray())
                    {
                        // Extract "id" and "displayName" properties
                        groupId = string.Empty;
                        displayName = string.Empty;

                        if (groupElement.TryGetProperty("id", out JsonElement idElement))
                        {
                            groupId = idElement.GetString();
                        }

                        if (groupElement.TryGetProperty("displayName", out JsonElement displayNameElement))
                        {
                            displayName = displayNameElement.GetString();
                        }

                        if (!string.IsNullOrEmpty(groupId))
                        {
                            // Use displayName or fallback to empty string if null
                            userGroupsDict[groupId] = displayName ?? string.Empty;
                            matchedAadGroupIds.Add(groupId);
                            _logger.Information($"Group ID: {groupId}, Display Name: {displayName}");
                        }
                    }
                }
                return matchedAadGroupIds;

            }
            catch (Exception ex)
            {
                _logger.Error("Error in GetUserInfoByIdAsync() method ", ex.Message);
                return null;
            }
        }

        public async Task<bool> SoftDeleteDocument(string contextUrl, InstitutionLibrary library, string itemPath, string location, string item)
        {
            bool delRes = true;
            dynamic siteId = null;
            dynamic libraryId = null;
            dynamic driveItem = null;
            try
            {
                _logger.Information("SoftDelete with contextUrl: {ContextUrl}, itemPath: {ItemPath}", contextUrl, itemPath);
                Requires.NotNullOrWhiteSpace(contextUrl, nameof(contextUrl));
                Requires.NotNullOrWhiteSpace(itemPath, nameof(itemPath));

                contextUrl = contextUrl.TrimEnd('/');

                string base64Url = Convert.ToBase64String(Encoding.UTF8.GetBytes(itemPath.TrimEnd())).Replace("/", "_").Replace("+", "-").TrimEnd('=');

                driveItem = await _graphClient.SpoInstance(itemPath.TrimEnd())
                        .Shares[$"u!{base64Url}"]
                        .DriveItem
                        .GetAsync();

                siteId = await _siteRepository.GetSiteIdAsync(contextUrl);
                libraryId = await _siteRepository.GetLibraryAsync(contextUrl, siteId, library);
                try
                {

                    await _graphClient.SpoInstance(contextUrl).Drives[libraryId].Items[driveItem.Id].DeleteAsync();
                }
                catch(Exception ex)
                {
                    try
                    {
                        driveItem = await _graphClient.SpoInstance(itemPath.TrimEnd())
                                .Shares[$"u!{base64Url}"]
                                .DriveItem
                                .GetAsync();

                        if (driveItem.Id != null)
                        {
                            _logger.Error("SoftDelete with contextUrl: {ContextUrl}, itemPath: {ItemPath} failed", contextUrl, itemPath);
                            return delRes = false;
                        }
                    }
                    catch(Exception ext)
                    {
                        _logger.Information("SoftDelete with contextUrl: {ContextUrl}, itemPath: {ItemPath} done", contextUrl, itemPath);
                        return delRes;
                    }
                }
                return delRes;
            }
            catch (Exception ex)
            {
                _logger.Error("SoftDelete with contextUrl: {ContextUrl}, itemPath: {ItemPath} failed :: {msg}", contextUrl, itemPath, ex.Message);
                throw new Exception($"'{itemPath}' :: File not deletes");
            }
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
                        await _postgresRepository.UpdateArchiveQueueStatusByIdAsync(doc.id, "The requested resource library was not found", "Failed");
                        throw new HttpStatusCodeException(404, "The requested resource library was not found.");
                }
                await _postgresRepository.UpdateArchiveQueueStatusByIdAsync(doc.id, "", "Success");
                return res;
            }
            catch (HttpStatusCodeException ex)
            {
                await _postgresRepository.UpdateArchiveQueueStatusByIdAsync(doc.id, "Error --" + ex.Message, "Failed");
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
                    files = await GetFileVersions(doc.FullPath);
                }
                catch (Exception ex)
                {
                    throw new HttpStatusCodeException(404, "The requested resource was not found.");
                }
                var contentType = await GetContentType<T>(doc.FullPath, library, file, clientSite);
                List<MShareArchive> mDataList = await GetFileMetaInfoAsync<T>(
                                clientSite,
                                library,
                                    doc.FullPath, "/" + location + "/", file, clientId, country, LimitVersion, contentType);
                PutObjectResponse metadataResponse = null;
                bool duplicate = await _s3Repository.ExistObjectAsync(country + "/" + clientId + "/" + location + "/" + file);
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
                        var (fileStm, name) = await DownloadFileByUrlAsync(doc.FullPath, fileVersion.Id, fileVersion.Id == latestVersion.Id ? true : false);

                        using (fileStm)
                        {
                            //----------------------File Upload--------------------------------------------------
                            string docModifiedDate = string.Empty; // fileVersion.LastModifiedDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? null;
                            metadataResponse = await _s3Repository.S3UploadAsync(fileStm, country + "/" + clientId + "/" + location + "/" + file, doc.FullPath, docModifiedDate);
                            _logger.AddMethodName().Information("File {0} version {1} uploaded.", name, metadataResponse.VersionId);

                            var mData = mDataList[index];
                            mData.VersionId = metadataResponse.VersionId;
                            mData.IsPublishedVersion = index == 0 ? true : false;
                            mData.PublishedObjectId = index == 0 ? null : mDataList[(int)mDataList.Count() - 1].ObjectID;

                            var res = await _postgresRepository.InsertDataAsync(mData);
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
                        var chk = await _s3Repository.ExistObjectAsync(country + "/" + clientId + "/" + location + "/" + file);
                        if (chk)
                        {
                            var delRes = await SoftDeleteDocument(clientSite, library, doc.FullPath, location, delName);
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
                    var (fileStm, name) = await DownloadFileByUrlAsync(doc.FullPath, latestVersion.Id, true);

                    using (fileStm)
                    {
                        //----------------------File Upload--------------------------------------------------
                        string docModifiedDate = string.Empty; // latestVersion.LastModifiedDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? null;
                        metadataResponse = await _s3Repository.S3UploadAsync(fileStm, country + "/" + clientId + "/" + location + "/" + file, doc.FullPath, docModifiedDate);
                        _logger.AddMethodName().Information("File {0} uploaded.", name);

                        var mData = mDataList[mDataList.Count() - 1];
                        mData.VersionId = metadataResponse.VersionId;
                        mData.IsPublishedVersion = true;
                        mData.PublishedObjectId = null;

                        var res = await _postgresRepository.InsertDataAsync(mData);
                        if (res <= NoResult)
                        {
                            _logger.AddMethodName().Warning("Document deleted from SharePoint");
                            throw new HttpStatusCodeException(500, "Record is not inserted in the database.");
                        }

                        _logger.AddMethodName().Information("Metadata insert completed");
                        if (DeleteSource)
                        {
                            var chk = await _s3Repository.ExistObjectAsync(country + "/" + clientId + "/" + location + "/" + file);
                            if (chk)
                            {
                                var delRes = await SoftDeleteDocument(clientSite, library, doc.FullPath, location, name);
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
}