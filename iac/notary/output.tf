output "azure_resource_group_name" {
  value = azurerm_resource_group.example.name
}

output "azure_kubernetes_cluster_name" {
  value = azurerm_kubernetes_cluster.aks.name
}

output "azure_container_registry_name" {
  value = azurerm_container_registry.acr.name
}

output "azure_keyvault_name" {
  value = azurerm_key_vault.kv.name 
}

output "notary_signing_cert_data" {
  value = azurerm_key_vault_certificate.signingCert.certificate_data
}

output "signing_cert_name" {
  value = azurerm_key_vault_certificate.signingCert.name
}