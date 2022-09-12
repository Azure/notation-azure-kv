data "azurerm_client_config" "current" {}

data "azuread_client_config" "current" {}

resource "random_id" "main" {
  byte_length = 4
  prefix      = "notary-"
}

resource "azuread_application" "example" {
  display_name = "${random_id.main.hex}-sp"
  owners       = [data.azuread_client_config.current.object_id]
}

resource "azuread_service_principal" "example" {
  application_id               = azuread_application.example.application_id
  owners                       = [data.azuread_client_config.current.object_id]
}

resource "azuread_service_principal_password" "example" {
  service_principal_id = azuread_service_principal.example.object_id
}

# southcentralus is the only region that supports signatures at this time.
resource "azurerm_resource_group" "example" {
  name     = "${random_id.main.hex}-rg"
  location = "southcentralus"
}

resource "azurerm_key_vault" "kv" {
  name                        = "${random_id.main.hex}-kv"
  location                    = azurerm_resource_group.example.location
  resource_group_name         = azurerm_resource_group.example.name
  enabled_for_disk_encryption = true
  tenant_id                   = data.azurerm_client_config.current.tenant_id
  soft_delete_retention_days  = 7
  purge_protection_enabled    = false
  sku_name                    = "standard"
}

resource "azurerm_key_vault_access_policy" "spAccessPolicy" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azuread_service_principal.example.object_id

  key_permissions = [
    "Get", "Sign"
  ]

  secret_permissions = [
    "Get", "Set", "List", "Delete", "Purge"
  ]

  certificate_permissions = [
    "Get", "Create", "Delete", "Purge"
  ]
}

resource "azurerm_key_vault_access_policy" "currentUserAccesPolicy" {
  key_vault_id = azurerm_key_vault.kv.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = data.azuread_client_config.current.object_id

  key_permissions = [
    "Get", "Sign"
  ]

  secret_permissions = [
    "Get", "Set", "List", "Delete", "Purge"
  ]

  certificate_permissions = [
    "Get", "Create", "Delete", "Purge"
  ]
}

resource "azurerm_key_vault_secret" "spClientId" {
  name         = "AZURE-CLIENT-ID"
  value        = azuread_service_principal.example.application_id
  key_vault_id = azurerm_key_vault.kv.id
  depends_on = [
    azurerm_key_vault_access_policy.currentUserAccesPolicy
  ]
}

resource "azurerm_key_vault_secret" "spClientSecret" {
  name         = "AZURE-CLIENT-SECRET"
  value        = "${azuread_service_principal_password.example.value}"
  key_vault_id = azurerm_key_vault.kv.id
  depends_on = [
    azurerm_key_vault_access_policy.currentUserAccesPolicy
  ]
}

resource "azurerm_key_vault_secret" "tenantId" {
  name         = "AZURE-TENANT-ID"
  value        = data.azurerm_client_config.current.tenant_id
  key_vault_id = azurerm_key_vault.kv.id
  depends_on = [
    azurerm_key_vault_access_policy.currentUserAccesPolicy
  ]
}

resource "azurerm_container_registry" "acr" {
  name                    = replace("${random_id.main.hex}acr","-","")
  resource_group_name     = azurerm_resource_group.example.name
  location                = azurerm_resource_group.example.location
  sku                     = "Premium"
  admin_enabled           = true
  zone_redundancy_enabled = true
}

data "external" "token" {
  program = ["${path.module}/createtoken.sh"]
  query = {
    registry = "${azurerm_container_registry.acr.name}"
    tokenName = "exampletoken"
  }
  depends_on = [
    azurerm_container_registry.acr
  ]
}

resource "azurerm_key_vault_secret" "notationUser" {
  name         = "NOTATION-USERNAME"
  value        = data.external.token.result["name"]
  key_vault_id = azurerm_key_vault.kv.id
  depends_on = [
    data.external.token,
    azurerm_key_vault_access_policy.currentUserAccesPolicy
  ]
}

resource "azurerm_key_vault_secret" "notationPassword" {
  name         = "NOTATION-PASSWORD"
  value        = data.external.token.result["password"]
  key_vault_id = azurerm_key_vault.kv.id
  depends_on = [
    data.external.token,
    azurerm_key_vault_access_policy.currentUserAccesPolicy
  ]
}

resource "azurerm_kubernetes_cluster" "aks" {
  name                             = "${random_id.main.hex}-aks"
  location                         = azurerm_resource_group.example.location
  resource_group_name              = azurerm_resource_group.example.name
  dns_prefix                       = "${random_id.main.hex}-aks-dns"
  kubernetes_version               = "1.22.11"
  sku_tier                         = "Free"
  http_application_routing_enabled = true


  network_profile {
    network_plugin    = "kubenet"
    load_balancer_sku = "standard"
  }


  azure_active_directory_role_based_access_control {
    managed            = true
    azure_rbac_enabled = true
  }

  default_node_pool {
    name            = "default"
    node_count      = 1
    max_pods        = 110
    vm_size         = "Standard_D2_v2"
    os_disk_size_gb = 128
    os_sku          = "Ubuntu"
    zones           = ["1"]
  }

  service_principal {
    client_id     = azuread_service_principal.example.application_id
    client_secret = azuread_service_principal_password.example.value
  }
  depends_on = [
    azuread_service_principal.example
  ]
}

resource "azurerm_role_assignment" "linkAcrAks" {
  principal_id                     = azuread_service_principal.example.object_id
  role_definition_name             = "AcrPull"
  scope                            = azurerm_container_registry.acr.id
  skip_service_principal_aad_check = true
}

resource "azurerm_key_vault_certificate" "signingCert" {
  name         = "example"
  key_vault_id = azurerm_key_vault.kv.id

  certificate_policy {
    issuer_parameters {
      name = "Self"
    }

    key_properties {
      exportable = true
      key_size   = 2048
      key_type   = "RSA"
      reuse_key  = true
    }

    lifetime_action {
      action {
        action_type = "AutoRenew"
      }

      trigger {
        days_before_expiry = 30
      }
    }

    secret_properties {
      content_type = "application/x-pkcs12"
    }

    x509_certificate_properties {
      extended_key_usage = ["1.3.6.1.5.5.7.3.1","1.3.6.1.5.5.7.3.2","1.3.6.1.5.5.7.3.3"]

      key_usage = [
        "cRLSign",
        "dataEncipherment",
        "digitalSignature",
        "keyAgreement",
        "keyCertSign",
        "keyEncipherment",
      ]

      subject            = "CN=example.com"
      validity_in_months = 12
    }
  }
  depends_on = [
    azurerm_key_vault_access_policy.spAccessPolicy
  ]
}

resource "null_resource" "update_kube_config" {
  provisioner "local-exec" {
    command = "az aks get-credentials --name ${azurerm_kubernetes_cluster.aks.name} --resource-group ${azurerm_resource_group.example.name} --admin --only-show-errors --overwrite-existing"
  }
}
