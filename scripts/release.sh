#!/bin/bash

set -ex

# Check if the tag name is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <tag_name>"
    exit 1
fi

tag_name="$1"
version=${tag_name#v}
release_assets_dir="$(pwd)/bin/release_assets"

# get all artifacts path
mapfile -t artifacts < <(find "$(pwd)/bin/artifacts" -type f)

# create release_assets directory
mkdir -p "$release_assets_dir"
cd "$release_assets_dir"

# move artifacts to release_assets directory
mv "${artifacts[@]}" ./

# create checksums
shasum -a 256 -- * > "notation-azure-kv_${version}_checksums.txt"

# Create a release using GitHub CLI
if [[ "$tag_name" == *"-"* ]]; then
    # v1.0.0-rc.1 is a pre-release
    gh release create --title "${tag_name}" --prerelease --draft "${tag_name}" -- * 
else
    # v1.0.0 is a release
    gh release create --title "${tag_name}" --draft "${tag_name}" -- *
fi
