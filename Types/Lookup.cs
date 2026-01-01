namespace S3WebApi.Types;

public record Lookup(int Value)
{
    public int Value { get; set; } = Value;
    public string? Description { get; set; }

    public static Lookup Factory(int value, string? description)
    {
        return new Lookup(value)
        {
            Description = description
        };
    }
}
