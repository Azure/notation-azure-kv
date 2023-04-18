using System.Text.Encodings.Web;
using System.Text.Json;

namespace Notation.Plugin.Proto
{
    class PluginIO
    {
        /// <summary>
        /// Reads the input from standard input.
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
        /// Writes the output to standard output.
        /// If the stderr is true, the output will be written to standard error.
        /// else, the output will be written to standard output.
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
                Console.Error.WriteLine(jsonString);
            else
                Console.WriteLine(jsonString);
        }


    }
}