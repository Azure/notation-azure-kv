terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.19.1"
    }
    azuread = {
      source = "hashicorp/azuread"
      version = "2.28.0"
    }
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {}

data "azuread_client_config" "current" {}

resource "random_id" "main" {
  byte_length = 4
  prefix      = "notary-"
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

resource "azurerm_container_registry" "acr" {
  name                    = replace("${random_id.main.hex}acr","-","")
  resource_group_name     = azurerm_resource_group.example.name
  location                = azurerm_resource_group.example.location
  sku                     = "Premium"
  admin_enabled           = true
  zone_redundancy_enabled = true
}

resource "azurerm_kubernetes_cluster" "aks" {
  name                             = "${random_id.main.hex}-aks"
  location                         = azurerm_resource_group.example.location
  resource_group_name              = azurerm_resource_group.example.name
  dns_prefix                       = "${random_id.main.hex}-aks-dns"
  kubernetes_version               = "1.23.12"
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

  identity {
    type = "SystemAssigned"
  }
}


resource "azurerm_role_assignment" "linkAcrAks" {
  principal_id                     = azurerm_kubernetes_cluster.aks.kubelet_identity[0].object_id
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
      extended_key_usage = ["1.3.6.1.5.5.7.3.3"]

      key_usage = [
        "digitalSignature",
      ]

      subject            = "CN=example.com"
      validity_in_months = 12
    }
  }
  depends_on = [
    azurerm_key_vault_access_policy.currentUserAccesPolicy
  ]
}