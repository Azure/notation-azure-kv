#!/bin/sh
#
# This script should be run via curl:
#   To install Notation and Notation-azure-kv plugin
#       sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/install.sh)" -- notation azure-kv
#
#   To install Notation
#       sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/install.sh)" -- notation
#
#   To install Notation-azure-kv plugin
#       sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/install.sh)" -- azure-kv
#
set -e

osType="$(uname -s)"
case "${osType}" in
Linux*)
    pluginInstallPath="${HOME}/.config/notation/plugins/azure-kv"
    osType="Linux"
    ;;
Darwin*)
    pluginInstallPath="${HOME}/Library/Application Support/notation/plugins/azure-kv"
    osType="Darwin"
    ;;
*)
    echo "unsupported OS ${osType}"
    exit 1
    ;;
esac

archOut="$(uname -m)"
case "${archOut}" in
x86_64) archType="amd64" ;;  # MacOS os Linux amd64
aarch64) archType="arm64" ;; # Linux ARM
arm64) atchType="arm64" ;;   # MacOS ARM
*)
    echo "Unsupported architecture ${archType}"
    exit 1
    ;;
esac
echo "Installing on ${osType} ${archType}"

# defer cleanup temp directory
tempDir="$(mktemp -d)"
cleanup() {
    rm -rf $tempDir
}
trap cleanup EXIT

# set install directory
binDir="${HOME}/bin"

# downloadLatestBinary returns the downloaded target path with shared variable
# `targetPath`.
downloadLatestBinary() {
    owner=$1
    repo=$2

    echo "Collecting ${repo} latest release..."
    downloadLink="$(curl -sL \
        -H "Accept: application/vnd.github+json" \
        -H "X-GitHub-Api-Version: 2022-11-28" \
        https://api.github.com/repos/${owner}/${repo}/releases |
        awk "BEGIN{IGNORECASE = 1}/browser_download_url/ && /${osType}/ && /${archType}/;" | grep -v dev | head -1 | awk -F '"' '{print $4}')"
    curl -OL $downloadLink --progress-bar --output-dir $tempDir

    # return target path
    targetPath="${tempDir}/$(basename ${downloadLink})"
}

install() {
    tarPath=$1
    binaryName=$2
    installPath=$3
    mkdir -p $installPath

    # extract and install
    tar -zxf $tarPath -C $installPath $binaryName

    # check install
    if [ ! -f "$installPath/$binaryName" ]; then
        echo "Failed to install ${binaryName}"
        exit 1
    fi
}

installNotation=false
installAzureKV=false

for i in $*; do
    case $i in
    notation) installNotation=true ;;
    azure-kv) installAzureKV=true ;;
    *)
        echo "unknown argument: $i"
        exit 1
        ;;
    esac
done

# install notation
if [ $installNotation = true ]; then
    downloadLatestBinary notaryproject notation
    install $targetPath notation $binDir

    version=$($binDir/notation version | grep Version | awk -F ' ' {'print $2'})
    echo "Sucessfully installed notation-v$version to $binDir"
    echo "Run the command to add the notation to PATH:"
    echo "  export PATH=\$PATH:$binDir"
    echo ""
fi

# install notation-akv-plugin
if [ $installAzureKV = true ]; then
    pluginName=notation-azure-kv
    downloadLatestBinary Azure $pluginName
    install $targetPath $pluginName $pluginInstallPath

    # check pluign installation
    if [ -z "$($binDir/notation plugin list | grep azure-kv)" ]; then
        echo "Failed to install notation-azure-kv plugin, or Notation is missing"
        exit 1
    fi
    version=$($binDir/notation plugin list | grep azure-kv | awk -F '[' {'print $1'} | awk -F ' ' {'print $NF'})
    echo "Successfully installed notation-azure-kv-v$version to $pluginInstallPath"
    echo "Run the command to show the installed plugins:"
    echo "  $binDir/notation plugin list"
fi
