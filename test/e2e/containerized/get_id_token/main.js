import * as core from '@actions/core';
import * as fs from 'fs';

async function getFederatedToken() {
    const audience = core.getInput('audience', { required: false });
    const federatedToken = await core.getIDToken(audience);
    return federatedToken;
}

async function main() {
    try {
        const token = await getFederatedToken();
        fs.writeFileSync('./federated-token', token);
        console.log(`Federated Token written to ./federated-token`);
    } catch (error) {
        core.setFailed(`Action failed with error: ${error.message}`);
    }
}

main();