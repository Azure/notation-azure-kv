using System.Text.Encodings.Web;
using System.Text.Json;

namespace Notation.Plugin.Protocol
{
    class PluginIO
    {
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

        /// <summary>
        /// Writes the output to standard input/output.
        /// 
        /// <param name="resp">
        /// The response object to be written to standard output.
        /// </param>
        /// <param name="stderr">
        /// If true, the output will be written to standard error, 
        /// otherwise, the output will be written to standard output.
        /// </param>
        /// </summary>
        public static void WriteOutput(object resp, bool stderr=false)
        {
            var options = new JsonSerializerOptions
            {
                // The Notation reads the output as UTF-8 encoded and 
                // the JSON text will not be used in HTML, so skip the strict
                // escaping rule for readability.
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string jsonString = JsonSerializer.Serialize(resp, options);

            if (stderr)
            {
                Console.Error.WriteLine(jsonString);
            }
            else
            {
                Console.WriteLine(jsonString);
            }
        }
    }
}