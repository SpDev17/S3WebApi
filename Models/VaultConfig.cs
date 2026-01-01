using S3WebApi.Interfaces;

namespace S3WebApi.Models;

public class VaultConfig : IVaultConfig
{
    public string VaultUserNameKey { get; set; }

    public string VaultPasswordKey { get; set; }

    public bool KubernetesAuthEnabled { get; set; }

    public string KubernetesTokenFilePath { get; set; }

    public string KubernetesAuthRole { get; set; }

    public string KubernetesAuthUrl { get; set; }

    public string JwtToken { get; set; }

    public bool EnvVariableValueEncrypted { get; set; }
}