namespace S3WebApi.Models;

public class PostgresConnection
{
    public string PostgreSqlConnection { get; set; }
}

public class ArchiveJobRequest
{
    public string ruleId { get; set; }
    public int limit { get; set; }
    public string Country { get; set; }
    public bool LimitVersion { get; set; }
    public bool DeleteSource { get; set; }
}

public class AutoFillArchiveQueueRequest
{
    public int pageno { get; set; }

    public int limit { get; set; }

    public string ruleid { get; set; }
}

public class TableRequest
{
    public string tablename { get; set; }
    public string selectcolumn { get; set; }
    public string where { get; set; }
    public string orderby { get; set; }
    public string sortorder { get; set; }
    public string limit { get; set; }
    public string pageno { get; set; }
    public string cte { get; set; }
    public string groupby { get; set; }

    public IReadOnlyList<Npgsql.NpgsqlParameter>? Parameters { get; set; }
}
