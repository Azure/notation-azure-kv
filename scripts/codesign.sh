#!/bin/bash

set -e

# Check if the tag name is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <runtime>"
    exit 1
fi
runtime="$1"

cd ./artifacts/"$runtime"

artifact_name=$(ls *.tar.gz)
if [ -z "$artifact_name" ]; then
    echo "No artifact found for $runtime"
    exit 1
fi

# extract binary and remove the artifact
tar -xf "$artifact_name"
rm "$artifact_name"

# codesign and compress again
codesign -s - notation-azure-kv
tar --no-xattrs -czvf "$artifact_name" notation-azure-kv LICENSE
rm notation-azure-kv LICENSE

# share the artifact name between steps
if [[ ! -z "$GITHUB_ENV" ]]; then
    echo "${ARTIFACT_NAME}=${artifact_name}" >> $GITHUB_ENV
fi
