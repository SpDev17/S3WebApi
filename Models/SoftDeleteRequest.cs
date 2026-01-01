namespace S3WebApi.Models;

public class SoftDeleteRequest : PhaServiceRequest
{
    public string ObjectUrl { get; set; }

    public string ObjectName { get; set; }

    public bool IsDocument { get; set; }
}
