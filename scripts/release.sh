#!/bin/bash

set -e

# Check if the tag name is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <tag_name>"
    exit 1
fi

tag_name="$1"
version=${tag_name#v}

cd artifacts
checksum_name=$(pwd)/notation-azure-kv_${version}_checksums.txt

# create checksums
artifacts=(*)
for artifact in "${artifacts[@]}"; do
    shasum -a 256 "${artifact}" >>"${checksum_name}"
done

# Create a release using GitHub CLI
if [[ "$tag_name" == *"-"* ]]; then
    # v1.0.0-rc.1 is a pre-release
    gh release create --title "${tag_name}" --prerelease --draft "${tag_name}" "${artifacts[@]}" "${checksum_name}"
else
    # v1.0.0 is a release
    gh release create --title "${tag_name}" --draft "${tag_name}" "${artifacts[@]}" "${checksum_name}"
fi
