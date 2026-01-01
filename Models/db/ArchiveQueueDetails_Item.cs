using System;

public class ArchiveQueueDetails_Item
{
    public Guid id { get; set; }
    public string ObjectID { get; set; }
    public string CompanyNo { get; set; }
    public string DocumentGuidId { get; set; }
    public DateTime? DocumentCreatedDate { get; set; }
    public DateTime? DocumentModifiedDate { get; set; }
    public string FullPath { get; set; }
    public string RelativePath { get; set; }
    public string SiteCollection { get; set; }
    public string DocumentLibrary { get; set; }
    public object DocumentMetaData { get; set; } // You can replace 'object' with a specific class if known
    public DateTime? ExtractionDate { get; set; }
    public string RuleID { get; set; }
    public string Status { get; set; }
    public string Logs { get; set; }
    public int RetryCount { get; set; }
    public int FileSize { get; set; }
    public DateTime? Created { get; set; }
    public DateTime? Modified { get; set; }
}