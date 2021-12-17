# notation-azure-kv

Azure Provider for the Notary v2 [Notation CLI](https://github.com/notaryproject/notation)

## Getting Started:
The following summarizes the steps to configure the Azure Key Vault notation plugin, configure gatekeeper, sign and verify a container image to Azure Kubernetes Service

```bash
# Configure notation with the Azure Key Vault plugin
notation plugin add azure-kv ~/.config/notation/plugins/azure-kv/notation-azure-kv

# Add signing and verification keys to the notation configuration policy
notation key add --name $KEY_NAME --plugin azure-kv --id $KEY_ID --kms
notation cert add --name $KEY_NAME --plugin azure-kv --id $KEY_ID --kms

# Install ratify, with the verification key
helm install ratify ratify/charts/ratify \
        --set registryCredsSecret=regcred \
        --set ratifyTestCert=$PUBLIC_KEY
kubectl apply -f ./ratify/charts/ratify-gatekeeper/templates/constraint.yaml

# Remotely sign with Azure Key Vault
notation sign --key $KEY_NAME $IMAGE 

# Deploy the image, with Gatekeeper, Ratify and Notary v2 validation of the signed image
kubectl run net-monitor --image=$IMAGE -n demo
```

See [documentation for details on remote signing with Azure Key Vault, validating a deployment to AKS with Notation and Ratify](docs/nv2-sign-verify-aks.md).



## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft
trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
