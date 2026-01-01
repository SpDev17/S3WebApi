using Microsoft.AspNetCore.Mvc;
using S3WebApi.Models.Common;
using Amazon.S3;

namespace S3WebApi.Helpers
{
    public static class ApiExceptionHelper
    {
        public static IActionResult HandleException(Exception ex, Serilog.ILogger logger)
        {
            // Log the exception details
            logger.AddMethodName().Error("An unexpected error occurred. Message : {0}", ex.Message);
            string errorMessage = "An unexpected error occurred. Please try again later.";
            if (ex is AmazonS3Exception s3Ex)
            {
                if (s3Ex.ErrorCode == "NotFound")
                {
                    errorMessage = "The requested file was not found. Please verify the file in S3 bucket and try again.";
                }
                else
                {
                    errorMessage = "An error occurred while accessing the file storage.";
                }
            }
            else if (ex is UnauthorizedAccessException unauthorizedEx)
            {
                errorMessage = unauthorizedEx.Message;
            }

            // Create a generic error message for the client
            var response = ApiResponse<object>.Fail(errorMessage);

            // Return HTTP 500 Internal Server Error with the standardized response
            return new ObjectResult(response)
            {
                StatusCode = 500
            };
        }
    }
}