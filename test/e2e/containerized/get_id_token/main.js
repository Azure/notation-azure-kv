import * as core from '@actions/core'
import * as fs from 'fs'

async function getFederatedToken() {
  const audience = 'api://AzureADTokenExchange'
  const federatedToken = await core.getIDToken(audience)
  return federatedToken
}

async function main() {
  try {
    const token = await getFederatedToken()
    fs.writeFileSync("./federated_token", token)
    console.log(`Federated Token written to ./federated_token`)
  } catch (error) {
    core.setFailed(`Action failed with error: ${error.message}`)
  }
}

main()
