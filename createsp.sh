#!/bin/bash
#
# Check required environment variables
#
command -v jq
if [ $? != 0 ]; then
    echo "jq is not installed, please install jq (sudo apt install jq)"
    exit 10
fi

command -v az
if [ $? != 0 ]; then
    echo "Azure CLI is not installed, please install Azure CLI (sudo pip install azure-cli)"
    exit 10
fi 

if [ ! $yourpassword ]; then
    echo "Please set the environment variable yourpassword with a password for the service principal."
    exit -1
fi

if [ ! $yourspname ]; then
    echo "Please set the environment variable yourspname with a name for the service principal."
    exit -1
fi

if [ ! $targetrg ]; then
    echo "Please set the environment variable targetrg with a name for the resource group containing the snapshot."
    exit -1
fi

#
# Store stuff needed later on
#
accountJson=$(az account show --out json)
tenantId=$(echo $accountJson | jq -r '.tenantId')
subId=$(echo $accountJson | jq -r '.id')

# We scope the SP to one RG for this simple sample
scope="/subscriptions/$subId/resourceGroups/$targetrg"

#
# Create a service principal on the target resource group as contributor
#
spjson=$(az ad sp create-for-rbac --name $yourspname --password $yourpassword --out json)
spid=$(echo $spjson | jq -r '.appId')
spname=$(echo $spjson | jq -r '.name')

az role assignment create --assignee $spname --role 'Contributor' --scope $scope

#
# Output what is now needed
#
echo "Please call the program now with the following parameters"
echo "export spid=$spid"
echo "export sppwd=*** # The password you set to the environment variable yourpassword"
echo "export tenantid=$tenantId"
echo "export subid=$subId"
echo "dotnet run $targetrg yoursnapshotname"