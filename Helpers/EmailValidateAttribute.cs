using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace S3WebApi.Helpers
{
    public class EmailValidateAttribute : ValidationAttribute
    {
        private static readonly Regex Pattern =
            new(@"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var s = value as string;
            if (string.IsNullOrWhiteSpace(s))
                return ValidationResult.Success; // let [Required] handle empties

            s = s.Trim();

            if (Pattern.IsMatch(s))
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage ?? "Invalid email address.");
        }
    }
}
