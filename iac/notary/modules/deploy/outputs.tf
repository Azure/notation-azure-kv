output "resource_group_name" {
  value = azurerm_resource_group.example.name
}

output "key_vault_name" {
  value = azurerm_key_vault.kv.name
}
## Name of secrets not values
output "notation_username" {
  value = azurerm_key_vault_secret.notationUser.name
}

output "notation_password" {
  value = azurerm_key_vault_secret.notationPassword.name
}

output "container_registry_name" {
  value = azurerm_container_registry.acr.name
}

output "kubernetes_cluster_name" {
  value = azurerm_kubernetes_cluster.aks.name
}

output "signing_cert" {
  value = azurerm_key_vault_certificate.signingCert.name
}