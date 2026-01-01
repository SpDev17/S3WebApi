using Microsoft.AspNetCore.Mvc;
using S3WebApi.Models.Common;

namespace S3WebApi.Helpers
{
    public static class ApiResponseHelper
    {
        public static IActionResult BadRequest(string errorMessage)
        {
            var response = ApiResponse<object>.Fail(errorMessage);
            return new BadRequestObjectResult(response);
        }

        public static IActionResult Ok<T>(T data, string message)
        {
            var response = new ApiResponse<T>(data, true, message);
            return new OkObjectResult(response);
        }

        public static IActionResult NotFound(string errorMessage)
        {
            var response = ApiResponse<object>.Fail(errorMessage);
            return new NotFoundObjectResult(response);
        }

        public static IActionResult Unauthorized(string errorMessage)
        {
            var response = ApiResponse<object>.Fail(errorMessage);
            return new UnauthorizedObjectResult(response);
        }
    }
}