using System.Text.Encodings.Web;
using System.Text.Json;

namespace Notation.Plugin.Protocol
{
    public interface IPluginResponse
    {
        /// <summary>
        /// Serializes the response object to JSON string.
        /// </summary>
        public string ToJson();
    }

    class PluginIO
    {
        /// <summary>
        /// The <see cref="JsonSerializerOptions"/> is used by subclass.
        /// </summary>
        public static JsonSerializerOptions GetRelaxedJsonSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                // The Notation reads the output as UTF-8 encoded and 
                // the JSON text will not be used in HTML, so skip the strict
                // escaping rule for readability.
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        /// <summary>
        /// Notation will invoke plugins as executable, pass parameters using 
        /// command line arguments, and use standard IO streams to pass 
        /// request payloads. This method reads the input from standard input.
        ///
        /// <returns>
        /// The input string from standard input.
        /// </returns>
        /// </summary>
        public static string ReadInput()
        {
            string? inputJson = Console.ReadLine();
            if (inputJson == null)
            {
                throw new ValidationException("Standard input is empty");
            }
            return inputJson;
        }
    }
}
