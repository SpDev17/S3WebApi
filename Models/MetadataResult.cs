using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace S3WebApi.Models
{
    public class MetadataResult
    {
        public IEnumerable<dynamic> Records { get; set; }
        public int TotalCount { get; set; }
    }
}