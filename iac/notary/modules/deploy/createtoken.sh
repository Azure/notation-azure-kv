#!/bin/sh

# createtoken.sh
eval "$(jq -r '@sh "registry=\(.registry);tokenName=\(.tokenName)"')"

password=$(az acr token create \
        --name $tokenName \
        --registry $registry \
        --scope-map _repositories_admin \
        --query 'credentials.passwords[0].value' \
        --only-show-errors \
        --output tsv)

name=$(az acr token show -r $registry -n $tokenName --query name -o tsv --only-show-errors)

cat <<EOF
{
  "name": "$name",
  "password": "$password"
}
EOF