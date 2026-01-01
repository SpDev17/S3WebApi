using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace S3WebApi.Helpers
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class DateFormatAttribute : ValidationAttribute
    {
        private readonly string _dateFormat;

        public DateFormatAttribute(string dateFormat)
        {
            _dateFormat = dateFormat;
            ErrorMessage = $"Date must be in the format {_dateFormat}.";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                // Required attribute should handle null check
                return ValidationResult.Success;
            }

            var dateString = value as string;
            if (dateString == null)
            {
                return new ValidationResult(ErrorMessage);
            }

            if (DateTime.TryParseExact(dateString, _dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return ValidationResult.Success;
            }

            return new ValidationResult(ErrorMessage);
        }
    }
}
