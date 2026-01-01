namespace S3WebApi.Interfaces;

public interface IVaultServiceHelper
{
    Dictionary<string, string> GetSecret(Uri tokenEndPoint, Uri secretEndPoint, string secretsNamespace);
}
