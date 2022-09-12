output "azure_key_vault_name" {
  value = module.deploy.key_vault_name
}

output "azure_container_registry" {
  value = module.deploy.container_registry_name
}

output "signing_key_name" {
  value = module.deploy.signing_cert
}