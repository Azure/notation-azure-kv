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
    helm = {
      source  = "hashicorp/helm"
      version = "2.6.0"
    }
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = "2.13.0"
    }
    kubectl = {
      source  = "gavinbunney/kubectl"
      version = "1.14.0"
    }
  }
}

provider "azurerm" {
  features {}
}

provider "helm" {
  kubernetes {
    config_path = "~/.kube/config"
  }
}

provider "kubernetes" {
  config_path = "~/.kube/config"
}

provider "kubectl" {
  load_config_file = true
}

module "deploy" {
  source = "./modules/deploy"
}

module "configure" {
  source = "./modules/configure"
  resource_group_name = module.deploy.resource_group_name
  key_vault_name = module.deploy.key_vault_name
  notation_username = module.deploy.notation_username
  notation_password = module.deploy.notation_password
  container_registry_name = module.deploy.container_registry_name
  signing_cert = module.deploy.signing_cert
  kubernetes_cluster_name = module.deploy.kubernetes_cluster_name
}