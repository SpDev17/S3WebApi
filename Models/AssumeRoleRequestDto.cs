namespace S3WebApi.Models;

public class AssumeRoleRequestDto
{
    public string AccessKey { get; set; }
    public string SecretKey { get; set; }
    public string EndpointUrl { get; set; }
    public string RoleArn { get; set; }
    public string SessionName { get; set; }
    public string Region { get; set; }
    public string BucketName { get; set; }
}
