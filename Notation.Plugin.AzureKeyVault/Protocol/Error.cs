namespace Notation.Plugin.Protocol
{
    /// <summary>
    /// Error class defines the <a href="https://github.com/notaryproject/notaryproject/blob/main/specs/plugin-extensibility.md#error-codes-for-describe-key-and-generate-signature">error codes</a> 
    /// for describe-key and generate-signature commands and provides the method
    /// to output the error to standard error.
    /// </summary>
    class Error
    {
        // Any of the required request fields was empty, or a 
        // value was malformed/invalid. Includes condition where the key referenced by keyId was not found.
        public const string VALIDATION_ERROR = "VALIDATION_ERROR";
        // The contract version used in the request is unsupported.
        public const string UNSUPPORTED_CONTRACT_VERSION = "UNSUPPORTED_CONTRACT_VERSION";
        // Authentication/authorization error to use given key.
        public const string ACCESS_DENIED = "ACCESS_DENIED";
        // The operation to generate signature timed out and can be retried by Notation.
        public const string TIMEOUT = "TIMEOUT";
        // The operation to generate signature was throttles and can be retried by Notation.
        public const string THROTTLED = "THROTTLED";
        // Any general error that does not fall into previous error categories.
        public const string ERROR = "ERROR";

        /// <summary>
        /// Print the error to standard error.
        /// </summary>
        public static void PrintError(string errorCode, string errorMessage)
        {
            // the errorMessage may has 
            // "Path: $ | LineNumber: 0 | BytePositionInLine: 0." suffix for
            // exception's Message, so remove it.
            errorMessage = errorMessage.Split("Path: $ |")[0];
            var errorResponse = new
            {
                errorCode = errorCode,
                errorMessage = errorMessage
            };
            PluginIO.WriteOutput(errorResponse, stderr: true);
        }
    }

    /// <summary>
    /// Base class for plugin exceptions.
    /// </summary>
    public class PluginException : Exception
    {
        public string Code { get; }
        public PluginException(string message, string code = Error.ERROR) : base(message)
        {
            Code = code;
        }
    }

    /// <summary>
    /// Exception for validation errors.
    /// </summary>
    public class ValidationException : PluginException
    {
        public ValidationException(string message) : base(message, Error.VALIDATION_ERROR) { }
    }
}
