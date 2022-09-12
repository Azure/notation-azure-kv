#!/bin/sh

# installnotation.sh
osVersion=$(uname | tr '[:upper:]' '[:lower:]');

# Choose a binary
timestamp=20220121081115
commit=17c7607

# Check if ~/bin is there, if not create it 
[ -d ~/bin ] || mkdir ~/bin

# Download Notation from pre-release
curl -Lo notation.tar.gz https://github.com/notaryproject/notation/releases/download/feat-kv-extensibility/notation-feat-kv-extensibility-$timestamp-$commit.tar.gz > /dev/null 2>&1

# Extract notation
mkdir ./tmp
tar xvzf notation.tar.gz -C ./tmp > /dev/null 2>&1

# Copy the notation cli to your bin directory
tar xvzf ./tmp/notation_0.0.0-SNAPSHOT-${commit}_${osVersion}_amd64.tar.gz -C ~/bin notation > /dev/null 2>&1

# Clean up
rm -rf ./tmp notation.tar.gz

# Add bin to PATH
export PATH="$HOME/bin:$PATH"

# Install Azure Keyvault Plugin

# Create a directory for the plugin
[ -d ~/.config/notation/plugins/azure-kv ] || mkdir -p ~/.config/notation/plugins/azure-kv

# Download the plugin
curl -Lo notation-azure-kv.tar.gz \
    https://github.com/Azure/notation-azure-kv/releases/download/v0.1.0-alpha.1/notation-azure-kv_0.1.0-alpha.1_${osVersion}_amd64.tar.gz > /dev/null 2>&1

# Extract to the plugin directory    
tar xvzf notation-azure-kv.tar.gz -C ~/.config/notation/plugins/azure-kv notation-azure-kv > /dev/null 2>&1

# Add Azure Keyvault plugin to notation
notation plugin ls | grep azure-kv > /dev/null
if [[ $? -eq 1 ]]
then 
    notation plugin add azure-kv ~/.config/notation/plugins/azure-kv/notation-azure-kv
fi

# Clean up
rm -rf notation-azure-kv.tar.gz