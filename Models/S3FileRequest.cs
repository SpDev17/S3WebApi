using System.ComponentModel.DataAnnotations;
using S3WebApi.Helpers;

namespace S3WebApi.Models;

public class S3FileRequest
{
    [Required(ErrorMessage = "Country is required.")]
    [MinLength(1, ErrorMessage = "Country cannot be empty.")]
    [RegularExpression(@"\S+", ErrorMessage = "Country cannot be whitespace.")]
    public string Country { get; set; }

    [Required(ErrorMessage = "Urls are required.")]
    public List<string> Urls { get; set; }

    [Required(ErrorMessage = "LimitVersion is required.")]
    public bool LimitVersion { get; set; }

    [Required(ErrorMessage = "DeleteSource is required.")]
    public bool DeleteSource { get; set; }
}

public class FileDownloadRequest
{
    [Required(ErrorMessage = "Client_ID is required.")]
    public string Client_ID { get; set; }

    [Required(ErrorMessage = "File Name is required.")]
    public string FileName { get; set; }

    [Required(ErrorMessage = "Site Url is required.")]
    public string SiteURL { get; set; }

    [Required(ErrorMessage = "File Url is required.")]
    public string FileUrl { get; set; }

    [Required(ErrorMessage = "Bucket Name is required.")]
    public string BucketName { get; set; }

    [Required(ErrorMessage = "Email_ID is required.")]
    [EmailValidate(ErrorMessage = "Please enter a valid email address.")]
    public string Email_ID { get; set; }
}
