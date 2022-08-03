# Notary v2 - Verification with Gatekeeper, Ratify and AKS

After signing a container image with notation which resides in an Azure Registry (aka Azure Container Registry), often the next step would be to enforce a policy that only signed container images can run in your AKS cluster.

## Prerequisites

> * An AKS cluster [with access to pull images from Azure registry](https://docs.microsoft.com/azure/container-registry/authenticate-kubernetes-options)
> * Complete the [Build, Sign and Verify a container image using notation and certificate in Azure Key Vault article](https://docs.microsoft.com/azure/container-registry/container-registry-tutorial-sign-build-push)

This starts with the following basic Azure service configuration:

* An ACR with ORAS Artifacts Support, enabling graphs of supply chain artifacts
* An AKS instance, linked to ACR for pulling images

## Install Gatekeeper

In this step, Gatekeeper will be configured, enabling deployment policies.

1. Install Gatekeeper

    ```azurecli-interactive
    helm repo add gatekeeper https://open-policy-agent.github.io/gatekeeper/charts

helm install gatekeeper/gatekeeper  \
    --name-template=gatekeeper \
    --namespace gatekeeper-system --create-namespace \
    --set enableExternalData=true \
    --set validatingWebhookTimeoutSeconds=7
    ````

## Install Ratify

1. Capture the public key for verification

In the previous article we downloaded the certificate from AKV to the local trust store, but we'll use a similar command to get the PUBLIC_KEY in a format ratify needs for the purposes of this demo.  In a production capacity, we should have the 

```azure-cli
CERT_ID=$(az keyvault certificate show -n $KEY_NAME --vault-name $AKV_NAME --query 'id' -o tsv)
az keyvault certificate download --file $CERT_PATH --id $CERT_ID --encoding PEM
notation cert add --name $KEY_NAME $CERT_PATH
```

```azure-cli
export PUBLIC_KEY=$(az keyvault certificate show -n $KEY_NAME \
                    --vault-name $AKV_NAME \
                    -o json | jq -r '.cer' | base64 -d | openssl x509 -inform DER)
```

2. Install Ratify

```azurecli-interactive
helm repo add ratify https://deislabs.github.io/ratify
helm install ratify \
    ratify/ratify \
    --set ratifyTestCert="$PUBLIC_KEY"
```

The constraint template applied denies any pods which are not signed in the `demo` namespace.

## Apply policy and deploy images

Now that gatekeeper and ratify are installed, a K8sSignedImages gatekeeper constraint policy needs to be applied to enforce only allowing signed images to be deployed.  After applying this policy we will show a signed imaged deployed successfully and a non-signed or trusted image deployed with a failure.

1. Apply a constraint policy to only allow signed images in the default namespace

```azurecli-interactive
kubectl config set-context --current --namespace=default
kubectl apply -f https://deislabs.github.io/ratify/library/default/template.yaml
kubectl apply -f https://deislabs.github.io/ratify/library/default/samples/constraint.yaml
```

The constraint template applied denies any pods which are not signed in the `default` namespace.

2. Deploy a signed image to AKS - with a success

    This image was previously built and pushed to your ACR in [the previous article](https://docs.microsoft.com/azure/container-registry/container-registry-tutorial-sign-build-push#configure-environment-variables). Replace `myregistry` with your actual registry name.

    ```bash
    kubectl run net-monitor --image=myregistry.azurecr.io/net-monitor:v1
    kubectl get pods
    ```

3. Deploy an unsigned image to AKS - with a failure

    ```bash
    kubectl run testunsigned \
      --image=ratify.azurecr.io/testimage:unsigned
    ```

    The error message should look like:

    ```bash
    Error from server (Forbidden): admission webhook "validation.gatekeeper.sh" denied the request: [ratify-constraint] Subject failed verification: ratify.azurecr.io/testimage:unsigned
    ```

4. List the running pods

    ```bash
    kubectl get pods -A
    ```

## Reset

Clear up ratify and gatekeeper resources, leaving AKS in place

```bash
helm uninstall ratify
helm uninstall gatekeeper/gatekeeper
kubectl delete -f https://deislabs.github.io/ratify/library/default/template.yaml
kubectl delete -f https://deislabs.github.io/ratify/library/default/samples/constraint.yaml
```
