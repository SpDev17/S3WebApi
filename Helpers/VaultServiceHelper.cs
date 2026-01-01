using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using S3WebApi.Interfaces;
using S3WebApi.Models;

namespace S3WebApi.Helpers;

public class VaultServiceHelper : IVaultServiceHelper
{
    private readonly HttpClient _httpClient;

    private const string _mediaType = "application/json";

    private const string _envReplacement = "Cached by SecretStore";

    private readonly string _vaultPassword;

    private readonly string _vaultUsername;

    private readonly IVaultConfig _vaultConfig;

    public VaultServiceHelper(HttpClient httpClient, IVaultConfig vaultConfig)
    {
        _httpClient = httpClient;
        _vaultConfig = vaultConfig;
        LoadKubernetesAuthToken(_vaultConfig);
        if (!_vaultConfig.KubernetesAuthEnabled)
        {
            _vaultPassword = Environment.GetEnvironmentVariable(_vaultConfig.VaultPasswordKey);            

            Environment.SetEnvironmentVariable(_vaultConfig.VaultPasswordKey, "Cached by SecretStore");
            _vaultUsername = Environment.GetEnvironmentVariable(_vaultConfig.VaultUserNameKey);
            
            Environment.SetEnvironmentVariable(_vaultConfig.VaultUserNameKey, "Cached by SecretStore");
            Console.Write(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + " [Information] [SPDMS.DMSGlobalLayer.VaultServiceHelper] UserName: " + _vaultUsername);
        }
    }

    /// <summary>
    /// Validates the secrets namespace to ensure it contains only allowed characters.
    /// This helps prevent SSRF by restricting the input used in HTTP headers.
    /// </summary>
    /// <param name="secretsNamespace">The secrets namespace string to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public bool ValidateSecretsNamespace(string secretsNamespace)
    {
        if (string.IsNullOrWhiteSpace(secretsNamespace))
        {
            Console.WriteLine("Secrets namespace is null or empty.");
            return false;
        }

        // Allow only alphanumeric characters, dashes, and underscores
        var regex = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9-_]+$");
        if (!regex.IsMatch(secretsNamespace))
        {
            Console.WriteLine($"Secrets namespace '{secretsNamespace}' contains invalid characters.");
            return false;
        }

        // Optionally, add a whitelist check here if you have predefined allowed namespaces

        return true;
    }

    /// <summary>
    /// Retrieves secrets from Vault using validated inputs.
    /// </summary>
    /// <param name="tokenEndPoint">The token endpoint URI.</param>
    /// <param name="secretEndPoint">The secret endpoint URI.</param>
    /// <param name="secretsNamespace">The secrets namespace string.</param>
    /// <returns>A dictionary of secrets if successful; otherwise, an empty dictionary.</returns>
    public Dictionary<string, string> GetSecret(Uri tokenEndPoint, Uri secretEndPoint, string secretsNamespace)
    {
        if (!ValidateSecretsNamespace(secretsNamespace))
        {
            Console.WriteLine("Invalid secrets namespace provided. Aborting secret retrieval.");
            return new Dictionary<string, string>();
        }

        HttpRequestMessage request;
        if (_vaultConfig.KubernetesAuthEnabled)
        {
            Console.WriteLine("Using Kubernetes Auth");
            request = GetRequestForKubernetesAuth(secretsNamespace);
        }
        else
        {
            request = GetRequest(secretsNamespace, new Uri(Path.Combine(tokenEndPoint.ToString(), _vaultUsername)));
        }

        string text = FetchToken(request);
        if (string.IsNullOrEmpty(text))
        {
            return new Dictionary<string, string>();
        }

        return FetchSecret(text, secretsNamespace, secretEndPoint);
    }

    private HttpRequestMessage GetRequest(string secretsNamespace, Uri tokenEndpoint)
    {
        var value = new
        {
            password = _vaultPassword
        };
        return new HttpRequestMessage
        {
            RequestUri = tokenEndpoint,
            Method = HttpMethod.Post,
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json"),
            Headers = { { "X-Vault-Namespace", secretsNamespace } }
        };
    }

    private HttpRequestMessage GetRequestForKubernetesAuth(string secretsNamespace)
    {
        var value = new
        {
            role = _vaultConfig.KubernetesAuthRole,
            jwt = _vaultConfig.JwtToken
        };
        HttpRequestMessage result = new HttpRequestMessage
        {
            RequestUri = new Uri(_vaultConfig.KubernetesAuthUrl),
            Method = HttpMethod.Post,
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json"),
            Headers = { { "X-Vault-Namespace", secretsNamespace } }
        };
        Console.WriteLine("Role:" + _vaultConfig.KubernetesAuthRole);
        return result;
    }

    private string FetchToken(HttpRequestMessage request)
    {
        try
        {
            Console.WriteLine("");
            Console.WriteLine($"Sending Auth token request to {request.RequestUri}");
            using HttpResponseMessage httpResponseMessage = _httpClient.SendAsync(request).Result;
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                return JsonSerializer.Deserialize<TokenResponse>(httpResponseMessage.Content.ReadAsStringAsync().Result).auth.client_token;
            }

            Console.WriteLine(httpResponseMessage.Content.ReadAsStringAsync().Result.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString().Replace("\r", "").Replace("\n", ""));
        }

        return string.Empty;
    }

    private Dictionary<string, string> FetchSecret(string token, string secretsNamespace, Uri secretsEndpoint)
    {
        try
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage
            {
                RequestUri = secretsEndpoint,
                Method = HttpMethod.Get
            };
            httpRequestMessage.Headers.Add("X-Vault-Namespace", secretsNamespace);
            httpRequestMessage.Headers.Add("X-Vault-Token", token);
            using HttpResponseMessage httpResponseMessage = _httpClient.SendAsync(httpRequestMessage).Result;
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
            {
                string result = httpResponseMessage.Content.ReadAsStringAsync().Result;
                result = result.Remove(0, result.IndexOf("\"data\":{") + 8);
                result = result.Remove(0, result.IndexOf("\"data\":{") + 7);
                result = result.Remove(result.IndexOf("\"metadata\":{") - 1);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(result);
            }

            Console.WriteLine(httpResponseMessage.Content.ReadAsStringAsync().Result.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString().Replace("\r", "").Replace("\n", ""));
        }

        return new Dictionary<string, string>();
    }

    private void LoadKubernetesAuthToken(IVaultConfig vaultConfig)
    {
        string text = string.Empty;
        if (vaultConfig.KubernetesAuthEnabled && File.Exists(vaultConfig.KubernetesTokenFilePath))
        {
            text = File.ReadAllText(vaultConfig.KubernetesTokenFilePath);
            Console.WriteLine("K8s Token has been read successfully");
        }

        if (vaultConfig.KubernetesAuthEnabled && string.IsNullOrEmpty(text))
        {
            Console.WriteLine("Unable to find Kubernetes token path " + vaultConfig.KubernetesTokenFilePath + " or token is empty, so swiching to standard vault auth");
            vaultConfig.KubernetesAuthEnabled = false;
        }
        else
        {
            vaultConfig.JwtToken = text;
        }
    }
}