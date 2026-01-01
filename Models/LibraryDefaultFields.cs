namespace S3WebApi.Models;
public class LibraryDefaultFields
{
    private const string LibraryDefaultFieldsSection = "LibraryDefaultFields";

    public string Library { get; set; }
    public IEnumerable<string> Fields { get; set; }
    public IEnumerable<string> AdditionalFields { get; set; }

    public static IEnumerable<string> GetDefaultFields(IConfiguration configuration, string library)
    {
        var result =
            configuration
                .GetSection(LibraryDefaultFieldsSection).Get<List<LibraryDefaultFields>>()
                .FirstOrDefault(entry => entry.Library == library);

        if (result == null)
        {
            throw new InvalidOperationException($"Invalid default fields configuration -> Library: {library}");
        }

        var fields = result.Fields.ToList();

        fields.AddRange(result.AdditionalFields);

        return fields;
    }

    public static IEnumerable<string> GetAdditionalFields(IConfiguration configuration, string library)
    {
        var result =
            configuration
                .GetSection(LibraryDefaultFieldsSection).Get<List<LibraryDefaultFields>>()
                .FirstOrDefault(entry => entry.Library == library);

        if (result == null)
        {
            throw new InvalidOperationException($"Invalid additional fields configuration -> Library: {library}");
        }

        return result.AdditionalFields;
    }
}
