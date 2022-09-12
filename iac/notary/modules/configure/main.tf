data "azurerm_kubernetes_cluster" "example" {
  name                = var.kubernetes_cluster_name
  resource_group_name = var.resource_group_name
}

data "azurerm_resource_group" "example" {
  name = var.resource_group_name
}

data "azurerm_key_vault" "kv" {
  name                = var.key_vault_name
  resource_group_name = data.azurerm_resource_group.example.name
}

data "azurerm_key_vault_secret" "notationUsername" {
  name         = var.notation_username
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_secret" "notationPassword" {
  name         = var.notation_password
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_key_vault_certificate" "signingCert" {
  name         = var.signing_cert
  key_vault_id = data.azurerm_key_vault.kv.id
}

data "azurerm_container_registry" "acr" {
  name                = var.container_registry_name
  resource_group_name = data.azurerm_resource_group.example.name
}

resource "time_sleep" "wait_for_aks" {
  create_duration = "180s"
  depends_on = [
    data.azurerm_kubernetes_cluster.example
  ]
}

resource "null_resource" "configure_aks" {

  provisioner "local-exec" {
    command = <<EOT
      kubectl create namespace gatekeeper-system;
      kubectl create namespace demo;
      kubectl create secret docker-registry regcred \
      --docker-server=${data.azurerm_container_registry.acr.login_server} \
      --docker-username=${data.azurerm_key_vault_secret.notationUsername.name} \
      --docker-password=${data.azurerm_key_vault_secret.notationPassword.value} \
      --docker-email=someone@example.com
    EOT
  }
  depends_on = [
    time_sleep.wait_for_aks
  ]
}

resource "helm_release" "gatekeeper" {
  name       = "gatekeeper"
  repository = "https://open-policy-agent.github.io/gatekeeper/charts"
  chart      = "gatekeeper"
  namespace  = "gatekeeper-system"
  depends_on = [
    null_resource.configure_aks
  ]

  set {
    name  = "enableExternalData"
    value = "true"
  }

  set {
    name  = "validatingWebhookTimeoutSeconds"
    value = 7
  }
}

resource "helm_release" "ratify" {
  name       = "ratify"
  repository = "https://deislabs.github.io/ratify"
  chart      = "ratify"
  depends_on = [
    helm_release.gatekeeper
  ]

  set {
    name  = "registryCredsSecret"
    value = "regcred"
  }

  set {
    name  = "ratifyTestCert"
    value = data.azurerm_key_vault_certificate.signingCert.certificate_data
  }
}

resource "null_resource" "ratify_install" {

  provisioner "local-exec" {
    command = <<EOT
      curl -L https://deislabs.github.io/ratify/library/default/template.yaml -o template.yaml;
      curl -L https://deislabs.github.io/ratify/library/default/samples/constraint.yaml -o constraint.yaml;
      kubectl apply -f template.yaml;
      kubectl apply -f constraint.yaml;
      rm template.yaml constraint.yaml
    EOT
  }

  depends_on = [
    helm_release.ratify
  ]
}

resource "null_resource" "notation_install" {

  provisioner "local-exec" {
    command = "./modules/configure/installnotation.sh"
  }
}

resource "null_resource" "notation_configure" {
  provisioner "local-exec" {
    command = <<EOT
        # Use notation to add the key id to the kms keys and certs
        keyName=${data.azurerm_key_vault_certificate.signingCert.name}
        certId=${data.azurerm_key_vault_certificate.signingCert.id}
        keyId=$(az keyvault certificate show --name ${data.azurerm_key_vault_certificate.signingCert.name} --vault-name ${data.azurerm_key_vault.kv.name} --query kid -o tsv)
        
        notation key remove $keyName > /dev/null 2>&1
        notation key add --name $keyName --plugin azure-kv --id $keyId --kms
        notation cert remove $keyName > /dev/null 2>&1 
        notation cert add --name $keyName --plugin azure-kv --id $certId --kms
    EOT
  }
}