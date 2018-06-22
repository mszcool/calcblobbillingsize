#
# Check required environment variables and required tools
#
$cmdResult = Get-Command jq -ErrorAction SilentlyContinue
if ( !$cmdResult ) {
    Write-Host "Please install jq on your machine!"
    exit -1
}

$cmdResult = Get-Command az -ErrorAction SilentlyContinue
if ( ! $cmdResult ) {
    Write-Host "Please install the Azure CLI on your machine!"
    exit -1
}

if ( ! $yourpassword ) {
    Write-Host "Please set the environment variable yourpassword with a password for the service principal."
    exit -1
}

if ( ! $yourspname ) {
    Write-Host "Please set the environment variable yourspname with a name for the service principal."
    exit -1
}

if (! $targetrg ) {
    Write-Host "Please set the environment variable targetrg with a name for the resource group containing the snapshot."
    exit -1
}

#
# Store stuff needed later on
#
$accountJson=(az account show --out json)
$tenantId=($accountJson | jq -r '.tenantId')
$subId=($accountJson | jq -r '.id')

# We scope the SP to one RG for this simple sample
$scope="/subscriptions/$subId/resourceGroups/$targetrg"

#
# Create a service principal on the target resource group as contributor
#
$spjson=(az ad sp create-for-rbac --name "$yourspname" --password "$yourpassword" --out json)
$spid=($spjson | jq -r '.appId')
$spname=($spjson | jq -r '.name')

az role assignment create --assignee "$spname" --role "Contributor" --scope "$scope"

#
# Output what is now needed
#
Write-Host "Please call the program now with the following parameters"
Write-Host "`$env:spid=`"$spid`""
Write-Host "`$env:sppwd=`"***`" # The password you set to the environment variable yourpassword"
Write-Host "`$env:tenantid=`"$tenantId`""
Write-Host "`$env:subid=`"$subId`""
Write-Host "dotnet run `"$targetrg`" `"yoursnapshotname`""