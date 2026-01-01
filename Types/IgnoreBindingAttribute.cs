namespace S3WebApi.Types;

[AttributeUsage(AttributeTargets.Property)]
public class IgnoreBindingAttribute : Attribute
{
    public string? Reason { get; set; }
}
