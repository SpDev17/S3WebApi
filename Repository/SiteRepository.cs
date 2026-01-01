﻿using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Graph.Models;
using S3WebApi.Types;
using Microsoft.Extensions.Caching.Memory;
using Validation;
using S3WebApi.DMSAuth;
using S3WebApi.Interfaces;

namespace S3WebApi.Repository
{
    public class SiteRepository : ISiteRepository
    {
        private const string GraphSiteFormat = "{0}:{1}";

        private readonly IConfiguration _configuration;
        private readonly IFeatureFlagDataPort _featureFlag;
        private readonly ILogger _logger;
        private readonly IMemoryCache _cache;
        private readonly GraphClient _graphClient;

        public SiteRepository(
            IConfiguration configuration,
            IFeatureFlagDataPort featureFlag,
            IMemoryCache cache,
            GraphClient graphClient)
        {
            _configuration = configuration;
            _featureFlag = featureFlag;
            _cache = cache;
            _graphClient = graphClient;
        }

        public async Task<string> GetSiteIdAsync(string contextUrl)
        {
            Requires.NotNullOrWhiteSpace(contextUrl, nameof(contextUrl));

            var uri = new Uri(contextUrl);
            //Console.WriteLine("Retrieving site -> Site: {uri}", contextUrl);
            var sitePosition = string.Format(GraphSiteFormat, uri.Host, uri.AbsolutePath);
            var siteKey = $"Cache:{nameof(SiteRepository)}:{nameof(GetSiteIdAsync)}:{sitePosition.ToLower()}";
            var isCached = _cache.TryGetValue(siteKey, out string siteId);

            if (isCached)
                return siteId!;

            Site? site;

            try
            {
                Console.WriteLine("[Graph Executing] Get site");

                site = await _graphClient.SpoInstance(contextUrl).Sites[sitePosition].GetAsync();

                Console.WriteLine("[Graph Successful] Get site");
            }
            catch (ODataError error)
            {
                Console.WriteLine("[Graph Error] {message}", error.Message);

                throw;
            }

            siteId = site!.Id!;

            _cache.Set(siteKey, siteId, TimeSpan.FromMinutes(20));

            Console.WriteLine("Site retrieved and cached!");

            return siteId;
        }

        public async Task<string> GetLibraryAsync(string contextUrl, string siteId, InstitutionLibrary library)
        {
            Requires.NotNullOrWhiteSpace(siteId, nameof(siteId));

            //Console.WriteLine("Retrieving library -> Site: {site}, Library: {library}",siteId, library);

            var siteLibraryKey =
                $"Cache:{nameof(SiteRepository)}:{nameof(GetLibraryAsync)}:{siteId}:{library.ToString()}";

            var isCached = _cache.TryGetValue(siteLibraryKey, out string libraryId);

            if (isCached)
                return libraryId!;

            DriveCollectionResponse? libraries;

            try
            {
                Console.WriteLine("[Graph Executing] Get library");

                libraries = await _graphClient.SpoInstance(contextUrl).Sites[siteId].Drives.GetAsync();

                Console.WriteLine("[Graph Successful] Get library");
            }
            catch (ODataError error)
            {
                Console.WriteLine("[Graph Error] {message}", error.Message);

                throw;
            }

            if (libraries?.Value == null)
                throw new InvalidOperationException();

            libraryId = libraries.Value.First(f => f.Name == Utility.GetLibraryName(library)).Id!;

            _cache.Set(siteLibraryKey, libraryId, TimeSpan.FromMinutes(20));

            Console.WriteLine("Library retrieved and cached!");

            return libraryId;
        }
       
    }
}
