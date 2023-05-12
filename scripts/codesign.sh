#!/bin/bash
# The script is for macOS to codesign the binary.

set -e

cd ./bin/artifacts
num_targets=$(echo *.tar.gz | wc -w)
if [ "$num_targets" -ne 1 ]; then
    echo "Expect 1 artifact, but got $num_targets"
    exit 1
fi
artifact_name=$(ls ./*.tar.gz)

# extract binary and remove the artifact
tar -xf "$artifact_name"
rm "$artifact_name"

# codesign and compress again
codesign -s - notation-azure-kv
tar --no-xattrs -czvf "$artifact_name" notation-azure-kv LICENSE
rm notation-azure-kv LICENSE
