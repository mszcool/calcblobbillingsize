using System;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Compute.Fluent;
using Microsoft.Azure.Management.Compute.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace MszCool.Samples.RealBlobSize
{
    class Program
    {
        static void Main(string[] args)
        {
            //
            // First check the arguments
            //
            if (args.Length != 2)
            {
                System.Console.WriteLine("Usage: realblobsize [resource-group-name] [snapshot-name]");
                System.Environment.Exit(-1);
                return;
            }

            var resourceGroupName = args[0];
            var snapshotName = args[1];
            if (string.IsNullOrEmpty(resourceGroupName) || string.IsNullOrEmpty(snapshotName))
            {
                System.Console.WriteLine("You have to pass in both, a resource group name and a snapshot name!");
                System.Environment.Exit(-2);
                return;
            }

            // 
            // Get the required environment variables
            //
            var clientId = System.Environment.GetEnvironmentVariable("spid");
            var clientSecret = System.Environment.GetEnvironmentVariable("sppwd");
            var tenantId = System.Environment.GetEnvironmentVariable("tenantid");
            var subscriptionId = System.Environment.GetEnvironmentVariable("subid");

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(subscriptionId))
            {
                System.Console.WriteLine("Missing any of the required environment variables:");
                System.Console.WriteLine("- spid = client ID of the service principal");
                System.Console.WriteLine("- sppwd = client secret of the service principal");
                System.Console.WriteLine("- tenantid = Azure AD tenant ID of the AD used for authentication");
                System.Console.WriteLine("- subid = Target subscription ID");
                System.Environment.Exit(-2);
                return;
            }

            //
            // Now, authenticate with the service principal against the target AzureAD tenant
            //
            var creds = SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
            var azure = Azure.Configure()
                             .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                             .Authenticate(creds)
                             .WithSubscription(subscriptionId);

            // 
            // Next get the shared access signature for the snapshot
            //
            var sasUriString = azure.Snapshots.GrantAccess(resourceGroupName, snapshotName, AccessLevel.Read, 60 * 20);
            if (string.IsNullOrEmpty(sasUriString))
            {
                System.Console.WriteLine("Failed granting access to managed snapshot!");
                System.Environment.Exit(-3);
                return;
            }

            var sasUri = new Uri(sasUriString);
            var accountName = sasUri.Host.Substring(0, sasUri.Host.IndexOf("."));
            var containerName = sasUri.Segments.GetValue(1).ToString().Replace("/", "");
            var blobName = sasUri.Segments.GetValue(2).ToString().Replace("/", "");
            var sasToken = sasUri.Query.ToString().Substring(1);

            CalculateBlobSize(accountName, containerName, blobName, sasToken, true);
        }

        //
        // Function for calculating the size of a blob
        //
        private static void CalculateBlobSize(string accountName, object containerName, object blobName, string sasToken, bool isDisk)
        {
            // Now that we hvae the SAS-token, let's calculate the size of the snapshot
            var blobSizeInBytes = 0F;

            System.Console.WriteLine("Trying to retrieve Blob...");
            var blob = default(ICloudBlob);
            var blobUri = new Uri($"https://{accountName}.blob.core.windows.net/{containerName}/{blobName}?{sasToken}");
            try
            {
                var reqOptions = new BlobRequestOptions
                {
                    ServerTimeout = TimeSpan.FromMinutes(5),
                    MaximumExecutionTime = TimeSpan.FromMinutes(5)
                };

                if (isDisk)
                {
                    var pageBlob = new CloudPageBlob(blobUri);
                    blob = pageBlob;

                    Task.WaitAll(blob.FetchAttributesAsync());

                    var blobSize = pageBlob.Properties.Length;

                    System.Console.WriteLine($"Blob size per API in bytes: {blobSize}.");
                    System.Console.WriteLine($"Blob size per API in MB: {blobSize / 1024 / 1024}.");
                    System.Console.WriteLine($"Blob size per API in GB: {blobSize / 1024 / 1024 / 1024}.");

                    var pageRanges = pageBlob.GetPageRangesAsync().Result;
                    foreach (var rg in pageRanges)
                    {
                        blobSizeInBytes += (12 + (rg.EndOffset - rg.StartOffset));
                    }

                }
                else
                {
                    var blockBlob = new CloudBlockBlob(blobUri);
                    blob = blockBlob;

                    blobSizeInBytes += 8;

                    var blockList = blockBlob.DownloadBlockListAsync().Result;
                    foreach (var bl in blockList)
                    {
                        blobSizeInBytes += bl.Length + bl.Name.Length;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"EXCPETION: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Console.WriteLine($"INNER EXCEPTION: {ex.InnerException.Message}");
                }
            }

            // Add the base size to the previously calculated size
            blobSizeInBytes += (124 + (blob.Name.Length * 2));
            foreach (var md in blob.Metadata)
            {
                blobSizeInBytes += (3 + (md.Key.Length + md.Value.Length));
            }

            // Output the final size of the blob
            System.Console.WriteLine("");
            System.Console.WriteLine($"Blob Size per calculation in bytes: {blobSizeInBytes}");
            System.Console.WriteLine($"Blob Size per calculation in MB:    {blobSizeInBytes / 1024 / 1024}");
            System.Console.WriteLine($"Blob Size per calculation in GB:    {blobSizeInBytes / 1024 / 1024 / 1024}");
        }
    }
}