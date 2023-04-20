using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Notation.Plugin.Protocol;

namespace Notation.Plugin.AzureKeyVault.Command
{
    /// <summary>
    /// Implementation of describe-key command.
    /// </summary>
    public class DescribeKey : IPluginCommand
    {
        private const string invalidInputError = "Invalid input. The valid input format is '{\"contractVersion\":\"1.0\",\"keyId\":\"https://<vaultname>.vault.azure.net/<keys|certificate>/<name>/<version>\"}'";

        public async Task<object> RunAsync(string inputJson)
        {
            // Deserialize JSON string to DescribeKeyRequest object
            DescribeKeyRequest? request = JsonSerializer.Deserialize<DescribeKeyRequest>(inputJson);
            if (request == null)
            {
                throw new ValidationException(invalidInputError);
            }

            // example uri: https://notationakvtest.vault.azure.net/keys/notationev10leafcert/847956cbd58c4937ab04d8ab8622000c
            var uri = new Uri(request.KeyId);

            // validate uri
            if (uri.Segments.Length != 4)
            {
                throw new ValidationException(invalidInputError);
            }
            if (uri.Segments[1] != "keys/" && uri.Segments[1] != "certificates/")
            {
                throw new ValidationException(invalidInputError);
            }
            if (uri.Scheme != "https")
            {
                throw new ValidationException(invalidInputError);
            }
            // extract keys|certificates name from the uri
            var name = uri.Segments[2].TrimEnd('/');
            var version = uri.Segments[3].TrimEnd('/');

            // generate a certificate client
            // TODO - This will be refactored when generate-signature command is
            // implemented.
            var credential = new AzureCliCredential();
            var dnsUri = new Uri($"{uri.Scheme}://{uri.Host}");
            var certificateClient = new CertificateClient(dnsUri, credential);

            // parse the certificate to be X509Certificate2
            var cert = await certificateClient.GetCertificateVersionAsync(name, version);
            var x509 = new X509Certificate2(cert.Value.Cer);

            // extract key spec from the certificate
            var keySpec = KeySpecUtils.ExtractKeySpec(x509);
            var encodedKeySpec = KeySpecUtils.EncodeKeySpec(keySpec);

            // Serialize DescribeKeyResponse object to JSON string
            return new DescribeKeyResponse(
                keyId: request.KeyId,
                keySpec: encodedKeySpec);
        }
    }
}