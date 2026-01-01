namespace S3WebApi.APIRoutes
{
    public static class ApiRoutes
    {
        public const string Root = "api";
        public const string Version = "v1";
        public const string Base = Root + "/" + Version;

        public static class ControllerRoute
        {
            public const string Controller = Base + "/[controller]";
        }          
    }
}