#!/bin/bash

set -e

# Check if the tag name is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <tag_name>"
    exit 1
fi

tag_name="$1"
version=${tag_name#v}
artifacts_dir=$(pwd)/bin/artifacts

checksum_name="$artifacts_dir"/notation-azure-kv_${version}_checksums.txt

# create checksums
mapfile -t artifacts < <(find "$artifacts_dir" -type f)
for artifact in "${artifacts[@]}"; do
    (cd "$(dirname "${artifact}")" && shasum -a 256 "$(basename "${artifact}")" >>"${checksum_name}")
done

# Create a release using GitHub CLI
if [[ "$tag_name" == *"-"* ]]; then
    # v1.0.0-rc.1 is a pre-release
    gh release create --title "${tag_name}" --prerelease --draft "${tag_name}" "${artifacts[@]}" "${checksum_name}"
else
    # v1.0.0 is a release
    gh release create --title "${tag_name}" --draft "${tag_name}" "${artifacts[@]}" "${checksum_name}"
fi
