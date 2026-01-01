using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using S3WebApi.Helpers;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using S3WebApi.Helpers;

namespace S3WebApi.Models
{
    public class SearchCondition : IValidatableObject
    {
        [Required(ErrorMessage = "Client_ID is required.")]
        public string Client_ID { get; set; }

        [Required(ErrorMessage = "Email_ID is required.")]
        public string Email_ID { get; set; }

        [Required(ErrorMessage = "From Date is required.")]
        [DateFormat("yyyy-MM-dd")]
        public string FromDate { get; set; }

        [Required(ErrorMessage = "To Date is required.")]
        [DateFormat("yyyy-MM-dd")]
        public string ToDate { get; set; }

        [Required(ErrorMessage = "Conditions list is required.")]
        public List<Conditions> Conditions { get; set; }

        [Required(ErrorMessage = "Page no is required.")]
        public string PageNo {get; set;}

        [Required(ErrorMessage = "Limit is required.")]
        public string Limit {get; set;}

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            DateTime fromDateValue;
            DateTime toDateValue;

            bool isFromDateValid = DateTime.TryParseExact(FromDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out fromDateValue);
            bool isToDateValid = DateTime.TryParseExact(ToDate, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out toDateValue);

            if (isFromDateValid && isToDateValid)
            {
                if (fromDateValue > toDateValue)
                {
                    yield return new ValidationResult("ToDate should not be greater than FromDate.", new[] { nameof(ToDate), nameof(FromDate) });
                }
            }
        }
    }
    
    public class Conditions
    {        
        [Required(ErrorMessage = "SearchBy is required.")]
        public string SearchBy { get; set; }

        [Required(ErrorMessage = "InputValue is required.")]
        public string InputValue { get; set; }

        [Required(ErrorMessage = "AndOR is required.")]
        public string AndOR { get; set; }
    }
}
