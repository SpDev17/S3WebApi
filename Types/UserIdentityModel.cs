namespace S3WebApi.Types;

public class UserIdentityModel
{
    public UserIdentityModel(string? displayName, IDictionary<string, object>? details)
    {
        DisplayName = displayName;
        Details = details;
    }

    public string? DisplayName { get; set; }
    public IDictionary<string, object>? Details { get; set; }
}