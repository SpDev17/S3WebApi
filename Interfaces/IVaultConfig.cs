namespace S3WebApi.Interfaces;

public interface IVaultConfig
{
    string JwtToken { get; set; }

    bool KubernetesAuthEnabled { get; set; }

    string KubernetesAuthRole { get; set; }

    string KubernetesAuthUrl { get; set; }

    string KubernetesTokenFilePath { get; set; }

    string VaultPasswordKey { get; set; }

    string VaultUserNameKey { get; set; }

    bool EnvVariableValueEncrypted { get; set; }
}
