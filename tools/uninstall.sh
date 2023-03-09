#!/bin/sh
#
# This script should be run via curl:
#   To uninstall Notation and Notation-azure-kv plugin
#       sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/uninstall.sh)" -- notation azure-kv
#
#   To uninstall Notation
#       sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/uninstall.sh)" -- notation
#
#   To uninstall Notation-azure-kv plugin
#       sh -c "$(curl -fsSL https://raw.githubusercontent.com/Azure/notation-azure-kv/main/tools/uninstall.sh)" -- azure-kv
#

for i in $*; do
    case $i in
    notation*) 
        rm $HOME/bin/notation
        if [ $? = 0 ]; then
            echo "Successfully uninstalled notation"
        fi
        ;;
    azure-kv*)
        rm -r $HOME/.config/notation/plugins/azure-kv
        if [ $? = 0 ]; then
            echo "Successfully uninstalled notation-azure-kv plugin"
        fi
        ;;
    *)
        echo "unknown argument: $i"
    esac
done