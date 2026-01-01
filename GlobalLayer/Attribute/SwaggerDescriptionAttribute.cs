namespace S3WebApi.GlobalLayer.Attribute
{
    public class SwaggerDescriptionAttribute : System.Attribute
    {
        public string Description { get; }

        public SwaggerDescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
