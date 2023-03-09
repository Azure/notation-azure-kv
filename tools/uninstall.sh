#!/bin/sh
#
# This script should be run via curl:
#   To uninstall both Notation and Notation-azure-kv plugin
#       sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/uninstall.sh)" -- notation azure-kv
#
#   To uninstall Notation-azure-kv plugin
#       sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/uninstall.sh)" -- azure-kv
#
#   To clean notation configurations
#       sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/uninstall.sh)" -- config

osType="$(uname -s)"
case "${osType}" in
Linux*)
    configDir="${HOME}/.config/notation"
    ;;
Darwin*)
    configDir="${HOME}/Library/Application Support/notation"
    ;;
*)
    echo "unsupported OS ${osType}"
    exit 1
    ;;
esac


for i in $*; do
    case $i in
    notation*)
        rm "${HOME}/bin/notation" \
            && echo "Successfully uninstalled notation"
        ;;
    azure-kv*)
        rm -r "${configDir}/plugins/azure-kv" \
            && echo "Successfully uninstalled notation-azure-kv plugin"
        ;;
    config*)
        rm -f "${configDir}/trustpolicy.json" \
            && rm -f "${configDir}/signingkeys.json" \
            && rm -f "${configDir}/config.json" \
            && rm -rf "${configDir}/truststore" \
            && echo "Successfully cleaned notation configurations"
        ;;
    *)
        echo "unknown argument: $i"
        ;;
    esac
done
