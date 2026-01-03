using Newtonsoft.Json;
using S3WebApi.DMSAuth;
using S3WebApi.Interfaces;
using S3WebApi.Repository;
using S3WebApi.Helpers;
using S3WebApi.Services;
using S3WebApi.Models;
using S3WebApi.ArchiveLayer;
using Serilog;
using Serilog.Events;
using S3WebApi.GlobalLayer;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;

Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

// Stage 2: Full logger configured with application settings and services
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.WithProperty("AppName", "MShare-Archive-Solution")
    .Enrich.WithProperty("Env", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToLower())
    .Enrich.FromLogContext());



// Access configuration 
var configuration = builder.Configuration;

string kubernetesCluster = Environment.GetEnvironmentVariable("KUBERNETES_CLUSTER");
bool isEKS = !string.IsNullOrWhiteSpace(kubernetesCluster) && kubernetesCluster.ToLower().Contains("eks");
TimeSpan timeOutSpan = new TimeSpan(00, 00, Convert.ToInt32(configuration[ConfigurationPaths.StaticConfigurations_ServiceStartupTimeOut]));
var authSecret = new AuthSecret
{
    Certificates = new Dictionary<string, string>()
};

//testing new setup
isEKS=true;
if (isEKS)
{
    // Step 1: Retrieve the JSON blob from the environment variable
    string kvListSpoidsJson = Environment.GetEnvironmentVariable("KV_LIST_SPOIDS");
    string kvListWorkerJson = Environment.GetEnvironmentVariable("KV_LIST_WORKER");
    //Separately handling both Vault KVs to easily identify which secret has not been read correctly.
    if (string.IsNullOrEmpty(kvListSpoidsJson))
    {
        Console.WriteLine("Error: KV_LIST_SPOIDS environment variable is not set.");
        Environment.Exit(-1);
    }

    if (string.IsNullOrEmpty(kvListWorkerJson))
    {
        Console.WriteLine("Error: KV_LIST_WORKER environment variable is not set.");
        Environment.Exit(-1);
    }

    // Step 2: Deserialize the JSON blob into a Dictionary<string, string>
    Dictionary<string, string> secretsSpoidsDictionary = null;
    try
    {
        secretsSpoidsDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(kvListSpoidsJson);
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Error parsing KV_LIST_SPOIDS JSON: {ex.Message}");
        Environment.Exit(-1);
    }

    Dictionary<string, string> secretsWorkerDictionary = null;
    try
    {
        secretsWorkerDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(kvListWorkerJson);
    }
    catch (JsonException ex)
    {
        Console.WriteLine($"Error parsing KV_LIST_WORKER JSON: {ex.Message}");
        Environment.Exit(-1);
    }

    // Step 3: Create an AuthSecret object and populate it with the secrets
    var allSecrets = new Dictionary<string, string>();

    // Add secrets from the Spoids dictionary
    foreach (var kvp in secretsSpoidsDictionary)
    {
        if (!allSecrets.ContainsKey(kvp.Key))
        {
            allSecrets[kvp.Key] = kvp.Value;
        }
        else
        {
            Console.WriteLine($"Warning: Duplicate key '{kvp.Key}' found in KV_LIST_SPOIDS. Skipping this entry.");
        }
    }

    // Add secrets from the Worker dictionary 
    foreach (var kvp in secretsWorkerDictionary)
    {
        if (!allSecrets.ContainsKey(kvp.Key))
        {
            allSecrets[kvp.Key] = kvp.Value;
        }
        else
        {
            Console.WriteLine($"Warning: Duplicate key '{kvp.Key}' found in KV_LIST_WORKER. Skipping this entry.");
        }
    }

    // Assign the combined secrets to the AuthSecret object
    authSecret.Certificates = allSecrets;

    // Step 4: Check if certificates are present
    if (!authSecret.Certificates.Any())
    {
        Console.WriteLine("Error: No certificates found in Vault KV.");
        Environment.Exit(-1);
    }
}
else
{
    IVaultServiceHelper secretService = new VaultServiceHelper(new HttpClient(), GetVaultConfig(configuration));
    Uri tokenEndpoint = ValidateUri(configuration[ConfigurationPaths.IntegrationAuthentication_TokenEndpoint]);
    Uri certificateEndpoint = ValidateUri(configuration[ConfigurationPaths.IntegrationAuthentication_CertificateEndpoint]);    
    authSecret.Certificates = secretService.GetSecret(
        tokenEndpoint,
        certificateEndpoint,
        configuration[ConfigurationPaths.IntegrationAuthentication_SecretsNamespace]);
    Console.WriteLine("Step 1: Auth read from vault secret");
    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + " [Information] [SPOWebservice.Startup] Certificate Count: " + authSecret.Certificates.Count);
    if (authSecret.Certificates.Count == 0)
    {
        Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + " [Error] [SPOWebservice.Startup] Could not get Certificates from the Vault. Terminating SPOWebservice.");
        Thread.Sleep(timeOutSpan);
        Environment.Exit(-1);
    }

    var mediationSecretService = new VaultServiceHelper(new HttpClient(), GetVaultConfig(configuration));
    Uri secretsEndpoint = ValidateUri(configuration[ConfigurationPaths.IntegrationAuthentication_SecretEndpoint]);    
    var secretsNamespaece = configuration[ConfigurationPaths.IntegrationAuthentication_SecretsNamespace];
    var auth = secretService.GetSecret(tokenEndpoint, secretsEndpoint, secretsNamespaece);
    if (auth is null || auth.Count == 0)
    {
        Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + " [mshare-controller] [Env] [Information] [Startup] Could not get authentication credentials from the Vault. Terminating Worker.");
        Thread.Sleep(timeOutSpan);
        Environment.Exit(-1);
    }

    foreach (KeyValuePair<string, string> item in auth)
    {
        if (!authSecret.Certificates.ContainsKey(item.Key))
            authSecret.Certificates.Add(item.Key, item.Value);
    }
}
authSecret.BuildMapping(configuration[ConfigurationPaths.AppPrincipal_CertSelector]);

// Add services to the container.
builder.Services.AddHttpClient("GitHub", httpClient =>
{
    httpClient.BaseAddress = new Uri("https://api.github.com/");
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddLogging();
builder.Services.AddMemoryCache();

// Register your services and repositories
// Make sure to register interfaces with their implementations
builder.Services.AddScoped<IS3Repository, S3Repository>();
builder.Services.AddScoped<IObjectStorageService, S3Service>();
builder.Services.AddScoped<ISharePointRepository, SharePointRepository>();
builder.Services.AddScoped<ISharePointService, SharePointService>();
builder.Services.AddScoped<Archive>();
builder.Services.AddScoped<ISiteRepository, SiteRepository>();
builder.Services.AddScoped<IDMSAuthOperation, DMSAuthOperation>();
builder.Services.AddScoped<IFeatureFlagDataPort, LaunchDarklyRepository>();
builder.Services.AddScoped<IDMSAuthOperation, DMSAuthOperation>();
//builder.Services.AddScoped<ITermMappingSwitcher, ChinaTermMappingSwitcher>();
builder.Services.AddSingleton<AuthSecret>(authSecret);
builder.Services.AddLogging();
builder.Services.AddHttpClient<IDMSAuthOperation, DMSAuthOperation>()
                .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                    new ThrottlingHandler(serviceProvider.GetRequiredService<IConfiguration>(), serviceProvider.GetRequiredService<IFeatureFlagDataPort>())
                    {
                        InnerHandler = new HttpClientHandler()
                    });

builder.Services.AddScoped<IPostgresRepository, PostgresRepository>();
builder.Services.AddScoped<IPostgresService, PostgresService>();

//builder.Services.AddScoped<ISpoAuthentication, SpoAuthentication>();
builder.Services.AddScoped<IChinaTermMappingSwitcher, ChinaTermMappingSwitcher>();
// China SPO services (Authentication and HTTP client for throttling management)
builder.Services.AddScoped<ISpoAuthentication, SpoAuthentication>();
builder.Services.AddHttpClient<ISpoAuthentication, SpoAuthentication>()
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
        new ThrottlingHandler(serviceProvider.GetRequiredService<IConfiguration>(), serviceProvider.GetRequiredService<IFeatureFlagDataPort>())
        {
            InnerHandler = new HttpClientHandler()
        });

// Register HttpClient for GraphClient
builder.Services.AddHttpClient<GraphClient>(client =>
{
    // Configure HttpClient if needed
});

// Add configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Services.AddMemoryCache();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "0.1.0",
        Title = "MShare365 API",
        Description =
            "MShare is a Document Management System (DMS) for maintaining client related documents " +
            "currently deployed as part of the Marsh Global SharePoint platform. " +
            "This API is designed to provide a set of web services to access Marsh documents and " +
            "to perform basic content management operations in SharePoint DMS.",
        Extensions =
            {
                {
                    // x-metadata is an additional attribute required by MMC API Catalog (Redocly).
                    "x-metadata", new OpenApiObject
                    {
                        {"category", new OpenApiString("Document Services")},
                        {"apiId", new OpenApiString("mshare365")},
                        {"BU", new OpenApiString("Marsh")},
                        {"tags", new OpenApiArray
                            {
                                new OpenApiString("Production")
                            }
                        },
                    }
                },
                {
                    "servers", new OpenApiArray()
                    {
                        new OpenApiObject
                        {
                            {"url", new OpenApiString("http://localhost:8080")},
                            {"description", new OpenApiString("DEV Apigee endpoint")}
                        },
                        //new OpenApiObject
                        //{
                        //    {"url", new OpenApiString("https://qa-api-mshareservices.mrshmc.com")},
                        //    {"description", new OpenApiString("QA Apigee endpoint")}
                        //},
                        //new OpenApiObject
                        //{
                        //    {"url", new OpenApiString("https://uat-api-mshareservices.mrshmc.com")},
                        //    {"description", new OpenApiString("UAT Apigee endpoint")}
                        //},
                        //new OpenApiObject
                        //{
                        //    {"url", new OpenApiString("https://api-mshareservices.mrshmc.com")},
                        //    {"description", new OpenApiString("PROD Apigee endpoint")}
                        //}
                    }
                }
            },
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Authorization Header is being used",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {new OpenApiSecurityScheme{Reference = new OpenApiReference
        {
                Id="Bearer",
                Type=ReferenceType.SecurityScheme
        }},new List<string>() }
    });
});

builder.Services.AddCors(opt =>
{
    opt.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
            .WithMethods("POST", "DELETE", "OPTIONS")
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "M-Share Web API - V1");
        c.SwaggerEndpoint("/swagger/v1/swagger.yaml", "M-Share Web API - V1 YAML");
    });
}

app.UseRouting();
app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});
app.Run();

static VaultConfig GetVaultConfig(IConfiguration configuration)
{
    Console.WriteLine("Step 1: Initiated call to read vault config data");
    const string AuthEnvConfigKey = "{authenv}";
    const string AuthEnvVariableKey = "AUTHENV";

    bool.TryParse(configuration["IntegrationAuthentication:KubernetesAuth:Enabled"], out bool k8sAuthEnabled);
    bool.TryParse(configuration["IntegrationAuthentication:ReadEncryptedValue"], out bool readEncrypted);
    var authUrl = configuration["IntegrationAuthentication:KubernetesAuth:AuthUrl"];

    if (authUrl.ToLower().Contains(AuthEnvConfigKey))
    {
        Console.WriteLine("Step 5: AuthURL contains AuthEnvConfigKey");
        var authEnvValue = Environment.GetEnvironmentVariable(AuthEnvVariableKey);
        Console.WriteLine("Step 6: Read AuthEnv value:", authEnvValue);
        if (!string.IsNullOrWhiteSpace(authEnvValue))
        {
            authUrl = authUrl.Replace(AuthEnvConfigKey, authEnvValue);
        }
        else
        {
            Console.WriteLine($"Unable to read environment variable value for {AuthEnvVariableKey}");
        }
    }


    return new VaultConfig
    {
        VaultUserNameKey = configuration["IntegrationAuthentication:HCVAULT_USERNAME"],
        VaultPasswordKey = configuration["IntegrationAuthentication:HCVAULT_PASSWORD"],
        KubernetesAuthEnabled = k8sAuthEnabled,
        KubernetesTokenFilePath = configuration["IntegrationAuthentication:KubernetesAuth:TokenFilePath"],
        KubernetesAuthRole = configuration["IntegrationAuthentication:KubernetesAuth:Role"],
        KubernetesAuthUrl = authUrl,
        EnvVariableValueEncrypted = readEncrypted
    };
}

static VaultConfig GetMediationVaultConfig(IConfiguration configuration)
{
    bool.TryParse(configuration["MediationAuthentication:KubernetesAuth:Enabled"], out bool k8sAuthEnabled);
    bool.TryParse(configuration["MediationAuthentication:ReadEncryptedValue"], out bool readEncrypted);

    return new VaultConfig
    {
        VaultUserNameKey = configuration["MediationAuthentication:HCVAULT_USERNAME"],
        VaultPasswordKey = configuration["MediationAuthentication:HCVAULT_PASSWORD"],
        KubernetesAuthEnabled = k8sAuthEnabled,
        KubernetesTokenFilePath = configuration["MediationAuthentication:KubernetesAuth:TokenFilePath"],
        KubernetesAuthRole = configuration["MediationAuthentication:KubernetesAuth:Role"],
        KubernetesAuthUrl = configuration["MediationAuthentication:KubernetesAuth:AuthUrl"],
        EnvVariableValueEncrypted = readEncrypted
    };
}

// Helper method to validate URIs to prevent SSRF
static Uri ValidateUri(string uriString)
{
    if (!string.IsNullOrWhiteSpace(uriString) && Uri.TryCreate(uriString, UriKind.Absolute, out Uri uriResult))
    {
        // Allow only HTTP and HTTPS schemes
        if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
        {
            return uriResult;
        }
    }
    throw new UriFormatException($"Invalid URI or unsupported scheme: {uriString}");
}
