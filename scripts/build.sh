#!/bin/bash

set -e

# Check if the tag name is provided
if [ -z "$1" ] | [ -z "$2" ]; then
    echo "Usage: $0 <tag_name> <runtime>"
    exit 1
fi

tag_name="$1"
version=${tag_name#v}
project_name="notation-azure-kv"
mkdir -p ./bin/publish && output_dir=$(realpath ./bin/publish)
mkdir -p ./bin/artifacts && artifacts_dir=$(realpath ./bin/artifacts)
runtime="$2"

# Publish for each runtime
commitHash="$(git log --pretty=format:'%h' -n 1)"
dotnet publish ./Notation.Plugin.AzureKeyVault \
    --configuration Release \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:CommitHash="$commitHash" \
    -p:Version="$version" \
    -r "$runtime" \
    -o "$output_dir/$runtime"

# Package the artifacts and create checksums
mkdir -p "${artifacts_dir}"
if [[ $runtime == *"win"* ]]; then
    ext="zip"
else
    ext="tar.gz"
fi

# Apply the runtime name mapping
mapped_runtime="${runtime/x64/amd64}"
mapped_runtime="${mapped_runtime/win/windows}"
mapped_runtime="${mapped_runtime/osx/darwin}"
mapped_runtime="${mapped_runtime/-/_}"
artifact_name="${artifacts_dir}/${project_name}_${version}_${mapped_runtime}.${ext}"
binary_dir="$output_dir/$runtime"

if [[ $ext == "zip" ]]; then
    # To have flat structured zip file, zip the binary and then update zip 
    # to include the LICENSE file
    (cd "${binary_dir}" && zip -x '*.pdb' -r "${artifact_name}" .) && zip -ur "${artifact_name}" LICENSE
else
    tar --no-xattrs -czvf "${artifact_name}" --exclude='*.pdb' -C "${binary_dir}" . -C ../../.. LICENSE
fi
