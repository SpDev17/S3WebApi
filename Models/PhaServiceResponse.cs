namespace S3WebApi.Models
{
    public class PhaServiceResponse
    {
        /// <summary>
        /// The process completed successfully
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// An optional message describing the reason for the Success value - commonly null when the process succeeds.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Optionally the ObjectId which was the subject of the process.  For example, the ID of the object inserted into sharepoint.
        /// </summary>
        public string ObjectId { get; }

        /// <summary>
        /// Optionally the exception thrown during process execution, usually indicates a failure.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Constructor for PhaServiceResponse.
        /// </summary>
        /// <remarks>
        /// Unless required by unusual circumstances, it is better to use one of the static methods:
        /// <li><see cref="SuccessResponse"/></li>
        /// <li><see cref="FailedResponse(string)"/></li>
        /// <li><see cref="FailedResponse(System.Exception)"/></li>
        /// <li><see cref="ObjectIdResponse(string, string, bool?)"/></li>
        /// <li><see cref="ObjectIdResponse(string, System.Exception, string, bool)"/></li>
        /// </remarks>
        /// <param name="message">A message describing the operation's success or failure - often null when a process succeeds.</param>
        /// <param name="success">True if the process was successful.</param>
        /// <param name="exception">Optionally holds an exception that was thrown during the process - usually indicates a failure.</param>
        /// <param name="objectId">Optionally holds the ObjectId which was the subject of the process.</param>
        public PhaServiceResponse(string message, bool success, Exception exception = null, string objectId = null)
        {
            Message = message;
            ObjectId = objectId;
            Success = success;
            Exception = exception;
        }

        /// <summary>
        /// Creates a successful response with an optional message.
        /// </summary>
        public static PhaServiceResponse SuccessResponse(string message = null) => new(message, true);

        /// <summary>
        /// Creates a failed response with an optional message.
        /// </summary>
        public static PhaServiceResponse FailedResponse(string message = null) => new(message, false);

        /// <summary>
        /// Creates a failed response from an exception, using the exception's message.
        /// </summary>
        public static PhaServiceResponse FailedResponse(Exception exception) => new(exception.Message, false, exception);

        /// <summary>
        /// Convenience method for creating responses with ObjectIds.  Success defaults to true when the objectId has a value.
        /// </summary>
        /// <param name="objectId">The ObjectID to include in the response.</param>
        /// <param name="message">(Optional) The message to include in the response.</param>
        /// <param name="success">(Optional) The success value to include in the response.  Defaults to true when the ObjectID has a value, or false if it is either null or empty.</param>
        /// <returns></returns>
        public static PhaServiceResponse ObjectIdResponse(string objectId, string message = null, bool? success = null)
            => new(message, success ?? !string.IsNullOrEmpty(objectId), null, objectId);

        /// <summary>
        /// Convenience method for creating exception based responses with ObjectIds.  Success defaults to false
        /// </summary>
        /// <param name="objectId">The ObjectID to include in the response.</param>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="message">Optionally a custom message for the response - otherwise defaults to the Exception.Message</param>
        /// <param name="success">Optionally the success value to return in the response - defaults to false.</param>
        /// <returns></returns>
        public static PhaServiceResponse ObjectIdResponse(string objectId, Exception exception, string message = null, bool success = false)
            => new(message ?? exception.Message, success, exception, objectId);

    }
}
