using System;

namespace S3WebApi.Helpers
{
    public static class ArgumentValidation
    {
        public static void RequireNotNullOrEmpty(this string value, string argumentName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("The value can't be null or empty", argumentName);
            }
        }

        public static void RequireNotNull(this object value, string argumentName)
        {
            if (value == null)
            {
                throw new ArgumentException("The value can't be null", argumentName);
            }
        }
    }
}
