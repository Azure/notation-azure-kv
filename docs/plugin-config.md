# Plugin Configuration
The `notation-azure-kv` plugin supports plugin configuration to get a valid cert chain or self-signed cert during remote signing.

## Usage
- Add the plugin configuration when adding the key
  ```sh
  notation key add \
    --plugin azure-kv \
    --id <key_identifier> \
    --plugin-config "<argument>=<value>" \
    --default
  ```
  Then sign the artifact
  ```sh
  notation sign <registry>/<repository>@<digest>
  ```

- Add the plugin config when sign the artifact
  ```sh
  notation sign <registry>/<repository>@<digest> \
    --plugin azure-kv \
    --id <key_identifier> \
    --plugin-config "<argument>=<value>"
  ```

## Supported arguments
### ca_certs
When the `--id` specifies a certificate that has multiple certificates in the certifcate chain, and the intermediate and root certificate were not [merged to Azure Key Vault](https://learn.microsoft.com//azure/key-vault/certificates/create-certificate-signing-request) or the chain was not complete, use `ca_certs` argument to pass the certificate bundle file path to the plugin.

Default: **empty string**

> **Note** Ensure that the certificates in the bundle are correctly ordered: starting from the intermediate certificate that signed the leaf certificate, and ending with the root certificate.
>
> They must be concatenated such that each certificate directly validates the preceding one. The following example features two certificates: an intermediate and a root certificate, but your certificate chain may include more or fewer.
>
> ```pem
> -----BEGIN CERTIFICATE-----
> Base64–encoded intermediate certificate
> -----END CERTIFICATE-----
> -----BEGIN CERTIFICATE-----
> Base64–encoded root certificate
> -----END CERTIFICATE-----
> ```
>
> **Note** To obtain your intermediate certificates and root certificate, you need to visit your Certificate Authority's official website. For example, if your certificate is signed by `digicert`, you should visit [DigiCert Trusted Root Authority Certificates
](https://www.digicert.com/digicert-root-certificates.htm) to download your certificates and manually build your certificate bundle based on the above description.

Example
```sh
notation sign <registry>/<repository>@<digest> \
  --plugin azure-kv \
  --id <key_identifier> \
  --plugin-config "ca_certs=/path/to/cert_bundle.pem"
```

### self_signed
When the `--id` specifies a self-signed certificate, use the `self_signed=true` argument.

Default: **false**

Example
```sh
notation sign <registry>/<repository>@<digest> \
  --plugin azure-kv \
  --id <key_identifier> \
  --plugin-config "self_signed=true"
```

## Permission management
The `notation-azure-kv` plugin support multiple level of permissions setting to satisfy different permission use cases.

- For certificate with chain, no configuration is required. The required permissions are `secrets get` and `sign`
- For certificate without chain, use `ca_certs` argument. The required permissions are `certificates get` and `sign`
- For self-signed certificate, use `self_signed=true` argument. The required permissions are `certificates get` and `sign`.
