# Notary v2 - Remote Signing and Verification with Gatekeeper, Ratify and AKS - Using Bicep


## Install the notation cli and azure-kv plugin

Starting from the root of this repo, make sure you are logged into the Azure CLI. This is a pre-requisite.

  ```bash
  az login
  ```

Set the desired subscription.

  ```bash
  az account set --subscription <id or name>
  ```

Once you have done the above, you are ready to deploy the required infrastructure by running the provided `setup.sh` script. The script will prompt you for the following:


1. Key Name: **Required** Key name used to sign and verify.
2. Key Subject Name: **Required** Key subject name used to sign and verify.


The script also supports overriding the following arguments, in the following order:
1. Resource Group Name: This will be the resource group created in Azure. If you do not provide a value `myakv-akv-rg` will be used.
2. Kubernetes Version: This is the version of Kubernetes control plane. If you do not provide a value, the latest Kubernetes version available for the provided location will be used.

The default location for this script is `southcentralus`. As of March 2022, this is the only region that supports [ORAS Artifact][oras-artifact] support, which enables storing signatures and other types of references.  
See [ACR Support for ORAS Artifacts][acr-oras-support] for more details.

Bash without overrides

  ```bash
  ./setup.sh
  ```
Bash with overrides

  ```bash
  ./setup.sh my-akvrg-override 1.23.3
  ```

### Clean up
To easily clean up the resources created from the setup script (I.E Resource Group, KeyVault, Azure Container Registry, Azure Kubernetes Service, and Service Principal), you may run the following script:

Bash
```
./cleanup.sh
```

### Things to note:
- To avoid issues with notation signing, acrName needs to be lowercase
- Local Notation KeyVault Plug In needs the following local environment variables (script will take care of this):
  - AZURE_CLIENT_SECRET
  - AZURE_CLIENT_ID
  - AZURE_TENANT_ID
- Local Notation CLI needs the following local environment variables (script will take care of this):
  - NOTATION_USERNAME
  - NOTATION_PASSWORD

### Known Issues / Currently Working on:

1. ~~Add installGatekeeper Function~~
2. ~~Add createSigningCertforKv Function~~
3. ~~Add buildImageandSign Function~~
4. ACR and AKS connection in Bicep

### Future todo list
1. Add support for pre-existing infrastructure (as of right now, this stands everything up from scratch in a brand new resource group)

[oras-artifact]:    https://github.com/oras-project/artifacts-spec
[acr-oras-support]: https://aka.ms/acr/oras-artifacts
