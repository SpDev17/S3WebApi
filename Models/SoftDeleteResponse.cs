namespace S3WebApi.Models
{
    /// <summary>Response object used by <see cref="Soft"/>  </summary>
    public class SoftDeleteResponse : PhaServiceResponse
    {
        /// <inheritdoc cref="PhaServiceResponse.Success"/>
        public bool Success { get; set; }

        /// <inheritdoc cref="PhaServiceResponse.Message"/>
        public string Message { get; set; } = "";

        /// <summary>
        /// WARNING:  If using this you MUST set a value for Success
        /// </summary>
        public SoftDeleteResponse() : base(null, false, null, null)
        { }
    }
}
