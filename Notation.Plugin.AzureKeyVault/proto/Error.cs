namespace Notation.Plugin.Proto
{
    /// <summary>
    /// Error codes for plugin.
    /// </summary>
    class Error
    {
        public const string VALIDATION_ERROR = "VALIDATION_ERROR";
        public const string UNSUPPORTED_CONTRACT_VERSION = "UNSUPPORTED_CONTRACT_VERSION";
        public const string ACCESS_DENIED = "ACCESS_DENIED";
        public const string TIMEOUT = "TIMEOUT";
        public const string THROTTLED = "THROTTLED";
        public const string ERROR = "ERROR";

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
