# Plugin Configuration
The `notation-azure-kv` plugin supports plugin configuration to use advanced features.

## Usage
- Add the plugin configuration when adding the key
  ```sh
  $ notation key add \
      --plugin azure-kv \
      --id <key_identifier> \
      --plugin-config <argument>=<value> \
      --default
  ```
  Then sign the artifact
  ```
  $ notation sign <artifact_reference>
  ```

- Add the plugin config when sign the artifact
  ```sh
  $ notation sign <artifact_reference> \
      --plugin azure-kv \
      --id <key_identifier> \
      --plugin-config <argument>=<value>
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

Example
```sh
$ notation sign <artifact_reference> \
    --plugin azure-kv \
    --id <key_identifier> \
    --plugin-config ca_certs=/path/to/cert_bundle.pem
```

### self_signed
When the `--id` specifies a self-signed certificate, use the `self_signed=true` argument.

Default: **false**

Example
```sh
$ notation sign <artifact_reference> \
    --plugin azure-kv \
    --id <key_identifier> \
    --plugin-config self_signed=true
```

## Permission management
The `notation-azure-kv` plugin support multiple level of permissions setting to satisfy different permission use cases. 

`Key Sign` permission is requred for remote signing, while `Secrets Get` and `Certificates Get` are optional based on use cases.

| `ca_certs` | `self_signed` | Key Vault Permission | Explain                                                                                            |
| ---------- | ------------- | -------------------- | -------------------------------------------------------------------------------------------------- |
| empty      | false         | `Secrets Get`        | Get the certificiate chain from Azure Key Vualt                                                    |
| empty      | true          | `Certificates Get`   | Get the self-signed certificate from Azure Key Vault                                               |
| exist      | false         | `Certificates Get`   | Get the leaf certificate from Azure Key Vault and fetch the certificate bundle from `ca_certs`     |
| exist      | true          | Exception            | If `self_signed` is true, the certificate chain should be empty, and `ca_certs` should not be set. |
