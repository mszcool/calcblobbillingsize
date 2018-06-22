Calculating the real size of a blob/snapshot
============================================

As per the official Azure Documentation on Managed Disks Snapshots pricing (look [here](https://azure.microsoft.com/en-us/pricing/details/managed-disks/) and search for snapshot), Snapshots are only billed against the truly consumed size of the disk stored in the snapshot.

Unfortunately, when opening using the Azure Portal, the Azure CLI or REST API for reviewing the Snapshot-size, it shows the "provisioned size" of the original disk, not the truly consumed size. The reason for that is simple: when restoring a snapshot to a disk, you get billed against the provisioned size for that real disk as documented in the link for Managed Disks Pricing mentioned above.

That leaves us with the challenge of calculating the size for a snapshot on our own to understand, what we will get billed against (or wait for the bill at the end of the month).

Inspired by [this PowerShell-based article](https://docs.microsoft.com/en-us/azure/storage/scripts/storage-blobs-container-calculate-billing-size-powershell) on the official Microsoft docs, I wrote this little sample with .NET Core that retrieves a shared access signature for managed disk snapshots and calculates its billing size. The sample uses a service principal which will get Contributor-access to a resource group in your subscription to automatically generate a SAS-token against a Managed Disk Snapshot and then calculate its real billing-size!

For using this sample, follow these steps:

* Make sure you have [.NET Core 2.1 installed](https://www.microsoft.com/net/download/dotnet-core/sdk-2.1.300) on your Linux, Mac or Windows machine.
* Make sure you have the [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest) and [jq](https://stedolan.github.io/jq/) installed on your machine (also works cross-platform on all above mentioned OS'es)
* Launch a terminal Window, change to the directory in which you cloned this repository, and do the following:
    * Set the variable `$yourspname` with a friendly name for a service principal to be created.
    * Set the variable `$yourpassword` with a password used for a service principal to be created.
    * Set the variable `$targetrg` to scope the permissions of the created service principal to a respective resource group. Of course, you can change the sample to allow access to multiple RGs or the entire subscription. But then you should reduce the RBAC permissions of that SP to follow "secure by default" principles.
* Sign-in with the Azure CLI by executing `az login` from the terminal window.
* Select the target subscription to work against using `az account set --subscription your-subscription-id`
* Execute the createsp-command for your platform:
    * on Linux or Mac, execute `createsp.sh`
    * on Windows, execute `createsp.ps1` in a Powershell-Window
* Follow the instructions from the output of the executed script