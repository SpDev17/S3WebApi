using System.Data;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using NpgsqlTypes;
using S3WebApi.DMSAuth;
using S3WebApi.Interfaces;
using S3WebApi.Models;

namespace S3WebApi.Repository
{
    public class PostgresRepository : IPostgresRepository
    {
        private readonly AuthSecret _authSecret;
        private readonly IConfiguration configuration;
        private readonly string _connectionString;
        private readonly PostgresConnection _settings;
        private Serilog.ILogger _logger => Serilog.Log.ForContext<PostgresRepository>();

        public PostgresRepository(IConfiguration configuration, AuthSecret authSecret)
        {
            _authSecret = authSecret;
            _settings = configuration.GetSection("ConnectionStrings").Get<PostgresConnection>();
            _authSecret.Certificates.TryGetValue(_settings.PostgreSqlConnection, out var _connectionStrings);
            _connectionString = _connectionStrings;
            //_connectionString = "Host=10.178.164.102;Port=5432;Database=mshareArchivalDB;Username=postgres;Password=mmc@123";
        }

        private IDbConnection Connection => new NpgsqlConnection(_connectionString);

        public async Task<int> InsertDataAsync(MShareArchive mData)
        {
            _logger.AddMethodName().Information("InsertDataAsync called with MShareArchive object: {@MShareArchive}", mData);
            try
            {

                using (
                    var conn = new NpgsqlConnection(_connectionString))
                {
                    int rowAffected = 0;
                    conn.Open();
                    var sql = @"Insert into public.mshare_archive(object_id, client_id, content_type, document_type_metadata, extended_metadata, date_of_archival, security_info, document_archive_location, storage_location, country, bucket_name, file_name, library_name, doc_created_date, doc_modified_date, version_id, ispublished_version, published_object_id)";
                    sql += " Values(@object_id, @client_id, @content_type, @document_type_metadata, @extended_metadata, CAST(@date_of_archival AS timestamp), @security_info, @document_archive_location, @storage_location, @country, @bucket_name, @file_name, @library_name, TO_TIMESTAMP(@doc_created_date::text, 'MM/DD/YYYY HH12:MI:SS PM'), TO_TIMESTAMP(@doc_modified_date::text, 'MM/DD/YYYY HH12:MI:SS PM'), @version_id, @ispublished_version, @published_object_id)";

                    using (var cmd = new NpgsqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("object_id", NpgsqlDbType.Text, (object?)mData.ObjectID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("client_id", NpgsqlDbType.Text, (object?)mData.ClientID ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("content_type", NpgsqlDbType.Text, mData.ContentType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("document_type_metadata", NpgsqlDbType.Jsonb, mData.DocumentTypeMetadata ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("extended_metadata", NpgsqlDbType.Jsonb, (object?)mData.ExtendedMetadata ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("date_of_archival", mData.DateOfArchival);
                        cmd.Parameters.AddWithValue("security_info", NpgsqlDbType.Text, mData.SecurityInfo ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("document_archive_location", NpgsqlDbType.Text, mData.DocumentArchiveLocation ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("storage_location", NpgsqlDbType.Text, mData.StorageLocation ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("country", NpgsqlDbType.Text, mData.Country ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("bucket_name", NpgsqlDbType.Text, mData.BucketName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("file_name", NpgsqlDbType.Text, mData.FileName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("library_name", NpgsqlDbType.Text, mData.LibraryName ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("doc_created_date", mData.DocCreatedDate);
                        cmd.Parameters.AddWithValue("doc_modified_date", mData.DocModifiedDate);
                        cmd.Parameters.AddWithValue("version_id", NpgsqlDbType.Text, (object?)mData.VersionId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("ispublished_version", NpgsqlDbType.Boolean, mData.IsPublishedVersion);
                        cmd.Parameters.AddWithValue("published_object_id", NpgsqlDbType.Text, (object?)mData.PublishedObjectId ?? DBNull.Value);


                        rowAffected = cmd.ExecuteNonQuery();
                    }
                    _logger.AddMethodName().Information("InsertDataAsync completed successfully, rows affected: {RowAffected}", rowAffected);
                    return rowAffected;

                }

            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error("Error executing InsertDataAsync : {0}", ex);
                throw;
            }
        }

        public async Task<int> UpdateArchiveQueueStatusAsync(string msg, string url, string? status = null)
        {
            _logger.AddMethodName().Information("UpdateArchiveQueueStatusAsync called with msg: {Msg}, url: {Url}, status: {Status}", msg, url, status);
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int rowAffected = 0;
                    conn.Open();
                    var sql = @"UPDATE public.archive_queue_details SET ""Logs"" = @Logs, ""Status"" = @Status  WHERE ""FullPath"" = @FullPath";

                    if (status != null)
                    {
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("Logs", "");
                            cmd.Parameters.AddWithValue("Status", "Success");
                            cmd.Parameters.AddWithValue("FullPath", url);
                            rowAffected = cmd.ExecuteNonQuery();
                        }
                        _logger.AddMethodName().Information("UpdateArchiveQueueStatusAsync completed successfully, rows affected: {RowAffected}", rowAffected);
                        return rowAffected;
                    }
                    else
                    {

                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("Logs", msg);
                            cmd.Parameters.AddWithValue("Status", "Failed");
                            cmd.Parameters.AddWithValue("FullPath", url);
                            rowAffected = cmd.ExecuteNonQuery();
                        }
                        _logger.AddMethodName().Information("UpdateArchiveQueueStatusAsync completed with failure status, rows affected: {RowAffected}", rowAffected);
                        return rowAffected;
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error("Error executing UpdateArchiveQueueStatusAsync : {0}", ex);
                throw;
            }
        }

        public async Task<MetadataResult> GetMedatadataDynamicAsync(SearchCondition searchCriteria, string permissions)
        {
            _logger.AddMethodName().Information("GetMedatadataDynamicAsync called with SearchCondition: {@SearchCondition}", searchCriteria);

            var parameters = new DynamicParameters();
            var whereClauses = new List<string>
            {
                // Base conditions
                "ispublished_version = true",
                "AND client_id = @ClientId"
            };

            parameters.Add("ClientId", searchCriteria.Client_ID.Trim());

            // Date filters
            if (!string.IsNullOrEmpty(searchCriteria.FromDate?.Trim()))
            {
                whereClauses.Add("AND doc_modified_date::date >= @FromDate");
                parameters.Add("FromDate", DateTime.Parse(searchCriteria.FromDate.Trim()));
            }
            if (!string.IsNullOrEmpty(searchCriteria.ToDate?.Trim()))
            {
                whereClauses.Add("AND doc_modified_date::date <= @ToDate");
                parameters.Add("ToDate", DateTime.Parse(searchCriteria.ToDate.Trim()));
            }

            // Dynamic conditions with parameters
            int conditionIndex = 0;
            foreach (var condition in searchCriteria.Conditions)
            {
                string conditionSql = GetParameterizedConditionSql(condition, parameters, ref conditionIndex);
                if (!string.IsNullOrEmpty(conditionSql))
                {
                    whereClauses.Add(conditionSql);
                }
            }

            string tableWhere = string.Join(" ", whereClauses);

            // Build the main SQL query string
            string sql = $@"
                {"WITH access_values AS (SELECT unnest(ARRAY[" + permissions + "]) AS access_value) "}
                SELECT 
                    CASE WHEN EXISTS (
                        SELECT 1 FROM jsonb_array_elements(security_info::jsonb) AS elem 
                        JOIN access_values av ON elem->>'Access' = av.access_value
                    ) THEN true ELSE false END AS can_access,
                    object_id, client_id, content_type, extended_metadata, date_of_archival, document_archive_location, storage_location, country, bucket_name, file_name, library_name,
                    doc_created_date, doc_modified_date, id, version_id, published_object_id
                FROM mshare_archive
                WHERE {tableWhere}
                OFFSET @Offset ROWS FETCH NEXT @Limit ROWS ONLY;
            ";

            // Add pagination parameters
            int offset = (Convert.ToInt32(searchCriteria.PageNo) - 1) * Convert.ToInt32(searchCriteria.Limit);
            parameters.Add("Offset", offset);
            parameters.Add("Limit", Convert.ToInt32(searchCriteria.Limit));

            using var conn = Connection;
            var records = await conn.QueryAsync<dynamic>(sql, parameters);

            // Build the count SQL query string
            string countSql = $@"
                {"WITH access_values AS (SELECT unnest(ARRAY[" + permissions + "]) AS access_value) "}
                SELECT COUNT(1) AS total_count
                FROM mshare_archive
                WHERE {tableWhere};
            ";

            var countResult = await conn.QueryAsync<dynamic>(countSql, parameters);
            int totalCount = 0;
            var countRecord = countResult.FirstOrDefault();
            if (countRecord != null)
            {
                totalCount = (int)countRecord.total_count;
            }

            _logger.AddMethodName().Information("GetMedatadataDynamicAsync completed successfully, retrieved {Count} records out of {TotalCount}", records.AsList().Count, totalCount);

            return new MetadataResult
            {
                Records = records,
                TotalCount = totalCount
            };
        }

        // Helper method to build parameterized condition SQL and add parameters
        private static string GetParameterizedConditionSql(Conditions condition, DynamicParameters parameters, ref int index)
        {
            switch (condition.SearchBy)
            {
                case "keyword":
                    {
                        string paramName = $"keyword{index++}";
                        parameters.Add(paramName, $"%{condition.InputValue.Trim().ToLower()}%");
                        return $"{condition.AndOR} lower(document_type_metadata::text) ILIKE @{paramName}";
                    }
                case "keywithvalue":
                    {
                        var inputs = condition.InputValue.Split(",");
                        if (inputs.Length < 2) return "";
                        string paramName = $"keyval{index++}";
                        parameters.Add(paramName, inputs[1].Trim().ToLower());
                        return $"{condition.AndOR} lower(document_type_metadata #>> '{{{inputs[0].Trim()}, Value}}') = @{paramName}";
                    }
                case "keyarrwithvalue":
                    {
                        var inputs = condition.InputValue.Split(",");
                        if (inputs.Length < 2) return "";
                        string paramName = $"keyarr{index++}";
                        parameters.Add(paramName, $"%{inputs[1].Trim().ToLower()}%");
                        return $"{condition.AndOR} EXISTS (SELECT 1 FROM jsonb_array_elements(document_type_metadata -> '{inputs[0].Trim()}') AS arr_object WHERE lower(arr_object ->> 'Label') ILIKE @{paramName})";
                    }
                default:
                    return "";
            }
        }

        public async Task<int> CreateDocDownloadLogAsync(FileDownloadRequest reqLog)
        {
            _logger.AddMethodName().Information("CreateDocDownloadLogAsync called with FileDownloadRequest: {@FileDownloadRequest}", reqLog);
            var sql = @"
                INSERT INTO file_request_log (user_id, file_url, bucket_name)
                VALUES (@Email_ID, @FileUrl, @BucketName)";
            using (var conn = Connection)
            {
                var result = await conn.ExecuteAsync(sql, reqLog);
                _logger.AddMethodName().Information("CreateDocDownloadLogAsync completed successfully, rows affected: {Result}", result);
                return result;
            }
        }

        public async Task<string> ValidateFileBeforeDownload(FileDownloadRequest fileReq)
        {
            _logger.AddMethodName().Information("ValidateFileBeforeDownload called with fileReq: {@fileReq}", fileReq);
            string sqlQuery = @"
            SELECT COUNT(1) as records 
            FROM public.mshare_archive
            WHERE client_id = @ClientId
            AND bucket_name = @BucketName
            AND file_name = @FileName
            AND lower(document_archive_location) LIKE LOWER('%' || @FileUrl || '%')
            LIMIT 1";

            using var conn = Connection;
            dynamic result = await conn.QueryAsync<dynamic>(sqlQuery, new
            {
                ClientId = fileReq.Client_ID,
                BucketName = fileReq.BucketName,
                FileName = fileReq.FileName,
                FileUrl = fileReq.FileUrl
            });

            _logger.AddMethodName().Information("ValidateFileBeforeDownload completed successfully, retrieved {result}", result);
            if (result is not null)
            {
                dynamic dapperRow = result[0];
                _logger.AddMethodName().Information("ValidateFileBeforeDownload completed successfully");
                return dapperRow.records == 0 ? "You have entered either the client id, file name, file url, or bucket name incorrectly!" : "";
            }
            else
            {
                _logger.AddMethodName().Information("ValidateFileBeforeDownload completed unsuccessfully");
                return "You have entered either the client id, file name, file url, or bucket name incorrectly!";
            }
        }

        public async Task<bool> GetPermissionByURL(FileDownloadRequest fileReq, List<SPGroupResponse> permissions)
        {
            _logger.AddMethodName().Information("GetPermissionByURL called with Permissions: {@permissions}", permissions);

            // Extract permission titles without manual quoting
            var permissionValues = permissions.Select(g => $"Group: {g.Title}").ToArray();

            string sql = @"
            WITH access_values AS (
                SELECT unnest(@Permissions) AS access_value
            )
            SELECT 
                CASE 
                    WHEN EXISTS (
                        SELECT 1 
                        FROM jsonb_array_elements(security_info::jsonb) AS elem 
                        JOIN access_values av ON elem->>'Access' = av.access_value
                    ) THEN true 
                    ELSE false 
                END AS can_access
            FROM public.mshare_archive
            WHERE client_id = @ClientId
            AND bucket_name = @BucketName
            AND file_name = @FileName
            LIMIT 1";

            using var conn = Connection;
            dynamic result = await conn.QueryAsync<dynamic>(sql, new
            {
                Permissions = permissionValues,
                ClientId = fileReq.Client_ID,
                BucketName = fileReq.BucketName,
                FileName = fileReq.FileName
            });
            _logger.AddMethodName().Information("GetPermissionByURL completed successfully, retrieved {result}", result);
            if (result is not null)
            {
                dynamic dapperRow = result[0];
                _logger.AddMethodName().Information("GetPermissionByURL completed successfully");
                return dapperRow.can_access;
            }
            else
            {
                _logger.AddMethodName().Information("GetPermissionByURL completed unsuccessfully");
                return false;
            }
        }

        public async Task<string> GetSiteURL(string client_Id)
        {
            _logger.AddMethodName().Information("GetSiteURL called for client : {@client_Id}", client_Id);
            string sql = "SELECT storage_location as site_url FROM public.mshare_archive WHERE client_id = @ClientId LIMIT 1;";
            using var conn = Connection;
            dynamic result = await conn.QueryAsync<dynamic>(sql, new { ClientId = client_Id });
            _logger.AddMethodName().Information("GetSiteURL completed successfully, retrieved {result}", result);
            if (result is not null && result.Count > 0)
            {
                dynamic dapperRow = result[0];
                _logger.AddMethodName().Information("GetSiteURL completed successfully");
                return dapperRow.site_url;
            }
            else
            {
                _logger.AddMethodName().Information("GetSiteURL completed unsuccessfully");
                return "";
            }
        }

        public async Task<IEnumerable<dynamic>> GetArchiveQueueAsync(string jobId, int limit)
        {
            var sql = @"
            SELECT 
                ""CompanyNo"",
                ""FullPath"",
                ""id""
            FROM public.archive_queue_details
            WHERE ""Status"" = 'New' 
            AND ""RuleID"" = @JobId
            ORDER BY ""DocumentCreatedDate"" ASC
            LIMIT @Limit";

            using var conn = Connection;
            return await conn.QueryAsync<dynamic>(sql, new { JobId = jobId, Limit = limit });
        }

        public async Task<int> GetArchiveQueueCountAsync(string jobId)
        {
            var sql = "SELECT COUNT(*) FROM public.archive_queue_details WHERE \"Status\" = 'New' AND \"RuleID\" = @JobId";
            using var conn = Connection;
            return await conn.QuerySingleAsync<int>(sql, new { JobId = jobId });
        }

        public async Task<IEnumerable<dynamic>> GetDataAsync(TableRequest table)
        {
            //SELECT * FROM your_table ORDER BY some_column LIMIT page_size OFFSET (page_number - 1) * page_size;
            if (!string.IsNullOrEmpty(table.tablename))
            {
                table.tablename = " public." + table.tablename;
            }
            if (!string.IsNullOrEmpty(table.where))
            {
                table.where = " where " + table.where;
            }
            if (!string.IsNullOrEmpty(table.orderby))
            {
                table.orderby = " ORDER BY " + table.orderby;
            }
            if (string.IsNullOrEmpty(Convert.ToString(table.limit)))
            {
                //table.limit = "limit " + 10;
            }
            else
            {
                table.limit = " limit " + table.limit;
            }
            if (!string.IsNullOrEmpty(Convert.ToString(table.pageno)))
            {
                table.pageno = " OFFSET (" + table.pageno + " - 1) * " + table.limit.Replace(" limit ", "") + " ";
            }
            var sql = " select " + table.selectcolumn + " from " + table.tablename + " " + table.where + " " + table.orderby + " " + table.sortorder + " " + table.limit + " " + table.pageno + "";
            if (!string.IsNullOrEmpty(Convert.ToString(table.cte)))
            {
                sql = table.cte + sql;
            }
            _logger.AddMethodName().Information("Constructed SQL query: {Sql}", sql);
            using var conn = Connection;
            return await conn.QueryAsync<dynamic>(sql);
        }

        public async Task<int> InsertArchiveQueueDetailsAsync(IDictionary<string, object> data)
        {
            var sql = @"
        INSERT INTO public.archive_queue_details(
            ""ObjectID"", ""DocumentCreatedDate"", ""DocumentModifiedDate"", ""FullPath"", 
            ""RelativePath"", ""SiteCollection"", ""CompanyNo"", ""DocumentLibrary"", ""DocumentMetaData"", 
            ""DocumentGuidId"", ""Status"", ""RuleID"",""Created"",""Modified"")
        VALUES (
            @ObjectID, @DocumentCreatedDate, @DocumentModifiedDate, @FullPath, 
            @RelativePath, @SiteCollection, @CompanyNo, @DocumentLibrary, @DocumentMetaData, 
            @DocumentGuidId, @Status, @RuleID,@Created,@Modified
        );
    ";

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(sql, conn);

                // Helper to add parameter or DBNull if missing
                void AddParam(string paramName, string key)
                {
                    if (data.TryGetValue(key, out var value) && value != null)
                        cmd.Parameters.AddWithValue(paramName, value);
                    else
                        cmd.Parameters.AddWithValue(paramName, DBNull.Value);
                }
                AddParam("ObjectID", "ObjectID");
                AddParam("DocumentCreatedDate", "DocumentCreatedDate");
                AddParam("DocumentModifiedDate", "DocumentModifiedDate");
                AddParam("FullPath", "FullPath");
                AddParam("RelativePath", "RelativePath");
                AddParam("SiteCollection", "SiteCollection");
                AddParam("CompanyNo", "CompanyNo");
                AddParam("DocumentLibrary", "DocumentLibrary");
                //AddParam("DocumentMetaData", "DocumentMetaData"); // jsonb type, pass as string or NpgsqlJsonb
                AddParam("DocumentGuidId", "DocumentGuidId");
                //AddParam("Logs", "Logs");
                //AddParam("BatchID", "BatchID");
                if (data.TryGetValue("DocumentMetaData", out object Document_MetaData))
                {
                    //filePathUrls.Add(Convert.ToString(FullPath));
                }

                AddParam("RuleID", "RuleID");
                cmd.Parameters.AddWithValue("Status", "New");
                cmd.Parameters.AddWithValue("Created", DateTime.Today);
                cmd.Parameters.AddWithValue("Modified", DateTime.Today);
                var docmetadata = new NpgsqlParameter("DocumentMetaData", NpgsqlDbType.Jsonb)
                {
                    Value = Document_MetaData
                };
                cmd.Parameters.Add(docmetadata);


                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                return rowsAffected;
            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error(ex, "Error inserting archive_queue_details");
                throw;
            }
        }

        public async Task<bool> BulkInsert_Documents_To_ArchiveQueue(IEnumerable<dynamic> data)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                using var writer = conn.BeginBinaryImport(@"
            COPY public.archive_queue_details (
                ""ObjectID"", ""DocumentCreatedDate"", ""DocumentModifiedDate"", ""FullPath"", 
                ""RelativePath"", ""SiteCollection"", ""CompanyNo"", ""DocumentLibrary"", ""DocumentMetaData"", 
                ""DocumentGuidId"", ""Status"", ""RuleID"", ""Created"", ""Modified""
            ) FROM STDIN (FORMAT BINARY)
        ");
                foreach (var item in data)
                {
                    var dict = (IDictionary<string, object>)item;
                    writer.StartRow();
                    object GetValue(string key) => dict.TryGetValue(key, out var value) ? value : DBNull.Value;
                    writer.Write(SanitizeToUtf8(GetValue("ObjectID")), NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(ToUtc(GetValue("DocumentCreatedDate")), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                    writer.Write(ToUtc(GetValue("DocumentModifiedDate")), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                    writer.Write(SanitizeToUtf8(GetValue("FullPath")), NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(SanitizeToUtf8(GetValue("RelativePath")), NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(SanitizeToUtf8(GetValue("SiteCollection")), NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(SanitizeToUtf8(GetValue("CompanyNo")), NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(SanitizeToUtf8(GetValue("DocumentLibrary")), NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(SanitizeToUtf8(GetValue("DocumentMetaData")), NpgsqlTypes.NpgsqlDbType.Jsonb);
                    writer.Write(SanitizeToUtf8(GetValue("DocumentGuidId")), NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(SanitizeToUtf8(GetValue("Status")), NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(SanitizeToUtf8(GetValue("RuleID")), NpgsqlTypes.NpgsqlDbType.Text);
                    writer.Write(ToUtc(DateTime.UtcNow), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                    writer.Write(ToUtc(DateTime.UtcNow), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                }
                writer.Complete();
                _logger.AddMethodName().Information("Bulk insert completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error("BulkInsert_Documents_To_ArchiveQueue encountered an exception : {0}", ex);
                return false;
            }
        }

        public async Task<bool> BulkUpsert_Documents_To_ArchiveQueue_old(IEnumerable<dynamic> data)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // 1️⃣ Create a temporary table identical to archive_queue_details
                var createTempTableSql = @"
            CREATE TEMP TABLE tmp_archive_queue_details AS
            SELECT * FROM public.archive_queue_details LIMIT 0;
        ";
                await using (var createCmd = new NpgsqlCommand(createTempTableSql, conn))
                {
                    await createCmd.ExecuteNonQueryAsync();
                    _logger.AddMethodName().Information("BulkUpsert_Documents_To_ArchiveQueue - Temp table created successfully");

                }

                // 2️⃣ COPY data into temp table
                using (var writer = conn.BeginBinaryImport(@"
            COPY tmp_archive_queue_details (
                ""ObjectID"", ""DocumentCreatedDate"", ""DocumentModifiedDate"", ""FullPath"", 
                ""RelativePath"", ""SiteCollection"", ""CompanyNo"", ""DocumentLibrary"", ""DocumentMetaData"", 
                ""DocumentGuidId"", ""Status"", ""RuleID"", ""Created"", ""Modified""
            ) FROM STDIN (FORMAT BINARY)
        "))
                {
                    foreach (var item in data)
                    {
                        var dict = (IDictionary<string, object>)item;
                        writer.StartRow();
                        object GetValue(string key) => dict.TryGetValue(key, out var value) ? value : DBNull.Value;

                        writer.Write(SanitizeToUtf8(GetValue("ObjectID")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(ToUtc(GetValue("DocumentCreatedDate")), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                        writer.Write(ToUtc(GetValue("DocumentModifiedDate")), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                        writer.Write(SanitizeToUtf8(GetValue("FullPath")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("RelativePath")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("SiteCollection")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("CompanyNo")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("DocumentLibrary")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("DocumentMetaData")), NpgsqlTypes.NpgsqlDbType.Jsonb);
                        writer.Write(SanitizeToUtf8(GetValue("DocumentGuidId")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("Status")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("RuleID")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(ToUtc(DateTime.UtcNow), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                        writer.Write(ToUtc(DateTime.UtcNow), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                    }

                    writer.Complete();
                    _logger.AddMethodName().Information("BulkUpsert_Documents_To_ArchiveQueue -bulk insert to Temp table successfull.");
                }

                // 3️⃣ Merge (upsert) from temp into main table
                var mergeSql = @"
            INSERT INTO public.archive_queue_details AS target (
                ""ObjectID"", ""DocumentCreatedDate"", ""DocumentModifiedDate"", ""FullPath"", 
                ""RelativePath"", ""SiteCollection"", ""CompanyNo"", ""DocumentLibrary"", ""DocumentMetaData"", 
                ""DocumentGuidId"", ""Status"", ""RuleID"", ""Created"", ""Modified""
            )
            SELECT 
                ""ObjectID"", ""DocumentCreatedDate"", ""DocumentModifiedDate"", ""FullPath"", 
                ""RelativePath"", ""SiteCollection"", ""CompanyNo"", ""DocumentLibrary"", ""DocumentMetaData"", 
                ""DocumentGuidId"", ""Status"", ""RuleID"", ""Created"", ""Modified""
            FROM tmp_archive_queue_details
            ON CONFLICT (""DocumentGuidId"") DO UPDATE
            SET 
                ""DocumentCreatedDate"" = EXCLUDED.""DocumentCreatedDate"",
                ""DocumentModifiedDate"" = EXCLUDED.""DocumentModifiedDate"",
                ""FullPath"" = EXCLUDED.""FullPath"",
                ""RelativePath"" = EXCLUDED.""RelativePath"",
                ""SiteCollection"" = EXCLUDED.""SiteCollection"",
                ""CompanyNo"" = EXCLUDED.""CompanyNo"",
                ""DocumentLibrary"" = EXCLUDED.""DocumentLibrary"",
                ""DocumentMetaData"" = EXCLUDED.""DocumentMetaData"",
                ""DocumentGuidId"" = EXCLUDED.""DocumentGuidId"",
                ""Status"" = EXCLUDED.""Status"",
                ""RuleID"" = EXCLUDED.""RuleID"",
                ""Modified"" = EXCLUDED.""Modified"";
        ";
                await using (var mergeCmd = new NpgsqlCommand(mergeSql, conn))
                {
                    await mergeCmd.ExecuteNonQueryAsync();
                }

                _logger.AddMethodName().Information("BulkUpsert_Documents_To_ArchiveQueue - Bulk upsert completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error("BulkUpsert_Documents_To_ArchiveQueue -failed, exception : {0}", ex);
                return false;
            }
        }
        public async Task<bool> BulkUpsert_Documents_To_ArchiveQueue(IEnumerable<dynamic> data)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                // 1️⃣ Create a temporary table identical to archive_queue_details
                var createTempTableSql = @"
            CREATE TEMP TABLE tmp_archive_queue_details AS
            SELECT * FROM public.archive_queue_details LIMIT 0;
        ";
                await using (var createCmd = new NpgsqlCommand(createTempTableSql, conn))
                {
                    await createCmd.ExecuteNonQueryAsync();
                    _logger.AddMethodName().Information("BulkUpsert_Documents_To_ArchiveQueue - Temp table created successfully");

                }

                // 2️⃣ COPY data into temp table
                using (var writer = conn.BeginBinaryImport(@"
            COPY tmp_archive_queue_details (
                ""ObjectID"", ""DocumentCreatedDate"", ""DocumentModifiedDate"", ""FullPath"", 
                ""RelativePath"", ""SiteCollection"", ""CompanyNo"", ""DocumentLibrary"", ""DocumentMetaData"", 
                ""DocumentGuidId"", ""Status"", ""RuleID"", ""Created"", ""Modified"",""FileSize"",""SiteTitle""
            ) FROM STDIN (FORMAT BINARY)
        "))
                {
                    foreach (var item in data)
                    {
                        var dict = (IDictionary<string, object>)item;
                        writer.StartRow();
                        object GetValue(string key) => dict.TryGetValue(key, out var value) ? value : DBNull.Value;

                        writer.Write(SanitizeToUtf8(GetValue("ObjectID")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(ToUtc(GetValue("DocumentCreatedDate")), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                        writer.Write(ToUtc(GetValue("DocumentModifiedDate")), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                        writer.Write(SanitizeToUtf8(GetValue("FullPath")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("RelativePath")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("SiteCollection")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("CompanyNo")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("DocumentLibrary")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("DocumentMetaData")), NpgsqlTypes.NpgsqlDbType.Jsonb);
                        writer.Write(SanitizeToUtf8(GetValue("DocumentGuidId")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("Status")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(SanitizeToUtf8(GetValue("RuleID")), NpgsqlTypes.NpgsqlDbType.Text);
                        writer.Write(ToUtc(DateTime.UtcNow), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                        writer.Write(ToUtc(DateTime.UtcNow), NpgsqlTypes.NpgsqlDbType.TimestampTz);
                        writer.Write(GetValue("FileSize"), NpgsqlTypes.NpgsqlDbType.Bigint);
                        writer.Write(SanitizeToUtf8(GetValue("SiteTitle")), NpgsqlTypes.NpgsqlDbType.Text);
                    }

                    writer.Complete();
                    _logger.AddMethodName().Information("BulkUpsert_Documents_To_ArchiveQueue -bulk insert to Temp table successfull.");
                }

                // 3️⃣ Merge (upsert) from temp into main table
                var mergeSql = @"
            INSERT INTO public.archive_queue_details AS target (
                ""ObjectID"", ""DocumentCreatedDate"", ""DocumentModifiedDate"", ""FullPath"", 
                ""RelativePath"", ""SiteCollection"", ""CompanyNo"", ""DocumentLibrary"", ""DocumentMetaData"", 
                ""DocumentGuidId"", ""Status"", ""RuleID"", ""Created"", ""Modified"",""FileSize"",""SiteTitle""
            )
            SELECT 
                ""ObjectID"", ""DocumentCreatedDate"", ""DocumentModifiedDate"", ""FullPath"", 
                ""RelativePath"", ""SiteCollection"", ""CompanyNo"", ""DocumentLibrary"", ""DocumentMetaData"", 
                ""DocumentGuidId"", ""Status"", ""RuleID"", ""Created"", ""Modified"",""FileSize"",""SiteTitle""
            FROM tmp_archive_queue_details
            ON CONFLICT (""DocumentGuidId"") DO NOTHING;           
        ";
                await using (var mergeCmd = new NpgsqlCommand(mergeSql, conn))
                {
                    await mergeCmd.ExecuteNonQueryAsync();
                }

                _logger.AddMethodName().Information("BulkUpsert_Documents_To_ArchiveQueue - Bulk upsert completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error("BulkUpsert_Documents_To_ArchiveQueue -failed, exception : {0}", ex);
                return false;
            }
        }

        private static object ToUtc(object value)
        {
            if (value == null || value is DBNull) return DBNull.Value;

            if (value is DateTime dt)
                return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();

            if (DateTime.TryParse(value.ToString(), out var parsed))
                return parsed.Kind == DateTimeKind.Utc ? parsed : parsed.ToUniversalTime();

            return DBNull.Value;
        }
        private static string SanitizeToUtf8(object value)
        {
            if (value == null || value is DBNull) return null;
            var str = value.ToString();
            var bytes = Encoding.Default.GetBytes(str);
            return Encoding.UTF8.GetString(bytes);
        }

        public async Task<IEnumerable<dynamic>> GetUrlInfo(List<string> urls)
        {
            try
            {
                _logger.AddMethodName().Information("GetUrlInfo started");

                var sql = @"SELECT ""id"", ""FullPath"", ""Status"" FROM public.archive_queue_details WHERE ""FullPath"" = ANY(@FullPath)";

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                var foundRecords = await conn.QueryAsync<dynamic>(sql, new { FullPath = urls.ToArray() });

                _logger.AddMethodName().Information("GetUrlInfo End");
                return foundRecords;
            }
            catch (Exception ex)
            {
                throw new Exception("Enable to process getting Database issue");
            }
        }

        static string QualifyPublic(string tableName)
        {
            tableName = (tableName ?? "").Trim();
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("TableName is required.", nameof(tableName));

            // Only allow simple identifiers (optional: extend/whitelist)
            if (!System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z_][\w\.]*$"))
                throw new ArgumentException("Invalid table name.");

            return tableName.StartsWith("public.", StringComparison.OrdinalIgnoreCase)
                ? tableName
                : $"public.{tableName}";
        }

        static string EnsureOrderBy(string? orderBy, string? sortOrder)
        {
            if (string.IsNullOrWhiteSpace(orderBy)) return "";
            var safeOrderBy = orderBy.Trim();

            // Optional: basic check to avoid injection in ORDER BY
            if (!System.Text.RegularExpressions.Regex.IsMatch(safeOrderBy, @"^[a-zA-Z_""\.,\s]+$"))
                throw new ArgumentException("Invalid ORDER BY.");

            var ord = string.IsNullOrWhiteSpace(sortOrder) ? "" : $" {sortOrder.Trim().ToUpperInvariant()}";
            return $" ORDER BY {safeOrderBy}{ord}";
        }

        public async Task<DataTable> GetTableDataAsync(TableRequest table)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            // ---- Validate / normalize ----
            if (string.IsNullOrWhiteSpace(table.tablename))
                throw new ArgumentException("TableName is required.", nameof(table.tablename));

            // Assume tables live in 'public'; let caller provide schema if needed
            var qualifiedTable = table.tablename.StartsWith("public.", StringComparison.Ordinal)
                ? table.tablename
                : $"public.{table.tablename}";

            var select = string.IsNullOrWhiteSpace(table.selectcolumn) ? "*" : table.selectcolumn;

            var whereClause = string.IsNullOrWhiteSpace(table.where) ? "" : $" WHERE {table.where}";
            var groupByClause = string.IsNullOrWhiteSpace(table.groupby) ? "" : $" GROUP BY {table.groupby}";
            var orderByClause = string.IsNullOrWhiteSpace(table.orderby) ? "" : $" ORDER BY {table.orderby}";
            var sortOrder = string.IsNullOrWhiteSpace(table.sortorder) ? "" : $" {table.sortorder}".TrimEnd();

            // Pagination
            string limitClause = "";
            string offsetClause = "";
            if (!string.IsNullOrEmpty(table.limit) && Convert.ToInt32(table.limit) is int limit && limit > 0)
            {
                limitClause = $" LIMIT {limit}";
                if (!string.IsNullOrEmpty(table.pageno) && Convert.ToInt32(table.pageno) is int page && page > 1)
                {
                    var offset = (page - 1) * limit;
                    offsetClause = $" OFFSET {offset}";
                }
            }

            // ---- Build final SQL ----
            var query =
                $"SELECT {select} " +
                $"FROM {qualifiedTable}" +
                $"{whereClause}" +
                $"{groupByClause}" +
                $"{orderByClause}" +
                $"{(string.IsNullOrEmpty(orderByClause) ? "" : sortOrder)}" + // append ASC/DESC only if ORDER BY present
                $"{limitClause}" +
                $"{offsetClause}";

            _logger.AddMethodName().Information("GetTableDataAsync query : {0}", query);

            var dt = new DataTable();

            using var cmd = new NpgsqlCommand(query, conn);

            // Bind parameters if provided
            if (table.Parameters is { Count: > 0 })
            {
                foreach (var p in table.Parameters)
                    cmd.Parameters.Add(p);
            }

            using var reader = await cmd.ExecuteReaderAsync();
            dt.Load(reader);
            return dt;
        }

        public async Task BulkUpsertDocumentDetailsAsync(IEnumerable<DocumentDetails_Item> items, CancellationToken ct = default)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();

            await using var tx = await conn.BeginTransactionAsync(ct);

            // 1) Create TEMP table (dropped automatically at commit/end of session)
            const string createTempSql = @"
            CREATE TEMP TABLE tmp_document_details (
                object_id              TEXT,
                company_no             TEXT,
                document_guid_id       TEXT,
                document_created_date  TIMESTAMPTZ,
                document_modified_date TIMESTAMPTZ,
                full_path              TEXT,
                relative_path          TEXT,
                site_collection        TEXT,
                document_library       TEXT,
                document_metadata      JSONB,
                extraction_date        TIMESTAMPTZ,
                rule_id                TEXT,
                status                 TEXT,
                file_size              INTEGER,
                sitetitle              TEXT
            ) ON COMMIT DROP;";
            await using (var createCmd = new NpgsqlCommand(createTempSql, conn, tx))
            {
                await createCmd.ExecuteNonQueryAsync(ct);
            }

            // 2) Binary COPY into temp table (fastest + type-safe)
            const string copySql = @"
            COPY tmp_document_details (
                object_id, company_no, document_guid_id, document_created_date, document_modified_date,
                full_path, relative_path, site_collection, document_library, document_metadata,
                extraction_date, rule_id, status, file_size,sitetitle
            ) FROM STDIN (FORMAT BINARY);";

            await using (var importer = await conn.BeginBinaryImportAsync(copySql))
            {
                foreach (var x in items)
                {
                    await importer.StartRowAsync(ct);
                    importer.Write(x.ObjectID, NpgsqlDbType.Text);
                    importer.Write(x.CompanyNo, NpgsqlDbType.Text);
                    importer.Write(x.DocumentGuidId, NpgsqlDbType.Text);
                    importer.Write(x.DocumentCreatedDate, NpgsqlDbType.TimestampTz);
                    importer.Write(x.DocumentModifiedDate, NpgsqlDbType.TimestampTz);
                    importer.Write(x.FullPath, NpgsqlDbType.Text);
                    importer.Write(x.RelativePath, NpgsqlDbType.Text);
                    importer.Write(x.SiteCollection, NpgsqlDbType.Text);
                    importer.Write(x.DocumentLibrary, NpgsqlDbType.Text);

                    var metaBytes = x.DocumentMetaData is null
                        ? null
                        : JsonSerializer.SerializeToUtf8Bytes(x.DocumentMetaData);
                    importer.Write(metaBytes, NpgsqlDbType.Jsonb);

                    importer.Write(DateTime.UtcNow, NpgsqlDbType.TimestampTz);
                    importer.Write(x.RuleID, NpgsqlDbType.Text);
                    importer.Write(x.Status, NpgsqlDbType.Text);
                    importer.Write(Convert.ToInt64(x.FileSize), NpgsqlDbType.Integer);
                    importer.Write(x.SiteTitle, NpgsqlDbType.Text);
                }

                await importer.CompleteAsync(ct);
            }

            // 3) Merge into target with ON CONFLICT (UPSERT)
            const string mergeSql = @"
            INSERT INTO document_details AS d (
                ""ObjectID"", ""CompanyNo"", ""DocumentGuidId"", ""DocumentCreatedDate"",""DocumentModifiedDate"",
                ""FullPath"", ""RelativePath"", ""SiteCollection"", ""DocumentLibrary"", ""DocumentMetaData"",
                ""ExtractionDate"", ""RuleID"", ""Status"", ""FileSize"",""SiteTitle""
            )
            SELECT
                object_id, company_no, document_guid_id, document_created_date, document_modified_date,
                full_path, relative_path, site_collection, document_library, document_metadata,
                extraction_date, rule_id, status, file_size,sitetitle
            FROM tmp_document_details
            ON CONFLICT (""DocumentGuidId"") DO NOTHING;";
            await using (var mergeCmd = new NpgsqlCommand(mergeSql, conn, tx))
            {
                await mergeCmd.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);
        }


        public async Task<int> UpdateArchiveQueueStatusByIdAsync(Guid Id, string msg, string? status = null)
        {
            _logger.AddMethodName().Information("UpdateArchiveQueueStatusAsync called with msg: {Msg}, url: {Id}, status: {Status}", msg, Id.ToString(), status);
            try
            {
                using (var conn = new NpgsqlConnection(_connectionString))
                {
                    int rowAffected = 0;
                    conn.Open();
                    var sql = @"UPDATE public.archive_queue_details SET ""Logs"" = @Logs, ""Status"" = @Status  WHERE ""id"" = @id";
                    if (status != null)
                    {
                        using (var cmd = new NpgsqlCommand(sql, conn))
                        {
                            if (status.ToLowerInvariant() == "success")
                            { cmd.Parameters.AddWithValue("Logs", ""); }
                            else
                            {
                                cmd.Parameters.AddWithValue("Logs", msg);
                            }

                            cmd.Parameters.AddWithValue("Status", status);
                            cmd.Parameters.AddWithValue("id", Id);
                            rowAffected = cmd.ExecuteNonQuery();
                        }
                        _logger.AddMethodName().Information("UpdateArchiveQueueStatusAsync completed with {status} successfully, rows affected: {RowAffected}", status, rowAffected);

                    }
                    return rowAffected;
                }

            }
            catch (Exception ex)
            {
                _logger.AddMethodName().Error(ex, "Error executing UpdateArchiveQueueStatusAsync");
                throw;
            }
        }

        public async Task<MetadataResult> GetDocumentVersionsIdAsync(string id)
        {
            _logger.AddMethodName().Information("GetDocumentVersionsIdAsync called with Id: {id}", id);
            string sqlQuery = @"
            WITH cte AS (
                SELECT object_id 
                FROM mshare_archive 
                WHERE id = @Id::uuid
            )
            SELECT ma.id, version_id, doc_modified_date 
            FROM cte 
            JOIN mshare_archive ma ON ma.published_object_id = cte.object_id 
            WHERE ispublished_version = false";

            using var conn = Connection;
            var result = await conn.QueryAsync<dynamic>(sqlQuery, new { Id = id.Trim() });
            if (result != null && result.Any())
            {
                _logger.AddMethodName().Information("GetDocumentVersionsIdAsync found {Count} records for id: {id}", result.Count(), id);
            }
            else
            {
                _logger.AddMethodName().Information("GetDocumentVersionsIdAsync found no records for id: {id}", id);
            }
            return new MetadataResult
            {
                Records = result,
                TotalCount = result.Count()
            };
        }
    }
}
