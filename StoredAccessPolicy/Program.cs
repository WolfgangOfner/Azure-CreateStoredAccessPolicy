using System;
using System.IO;
using System.Text;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace StoredAccessPolicy
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var storageAccount =
                CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));

            var blobClient = storageAccount.CreateCloudBlobClient();

            var container = blobClient.GetContainerReference("myblockcontainer");
            container.CreateIfNotExists();

            var containerSas = GetContainerSasUri(container);
            Console.WriteLine($"Container SAS URI: {containerSas}");

            var blobSas = GetBlobSasUri(container);
            Console.WriteLine($"Blob SAS URI: {blobSas}");

            var perms = container.GetPermissions();
            perms.SharedAccessPolicies.Clear();
            container.SetPermissions(perms);

            var sharedAccessPolicyName = "myPolicy";
            CreateSharedAccessPolicy(container, sharedAccessPolicyName);

            var containerSasWithAccessPolicy = GetContainerSasUriWithPolicy(container, sharedAccessPolicyName);
            Console.WriteLine($"Container SAS URI using stored access policy: {containerSasWithAccessPolicy}");

            var blobSasWithAccessPolicy = GetBlobSasUriWithPolicy(container, sharedAccessPolicyName);
            Console.WriteLine($"Blob SAS URI using stored access policy: {blobSasWithAccessPolicy}");
        }

        private static string GetContainerSasUri(CloudBlobContainer container)
        {
            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.List | SharedAccessBlobPermissions.Write
            };

            var sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            return container.Uri + sasContainerToken;
        }

        private static string GetBlobSasUri(CloudBlobContainer container)
        {
            var blob = container.GetBlockBlobReference("blobForSAS.txt");

            blob.UploadText("This blob will be accessible to clients via a shared access signature (SAS).");

            var sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-5),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
            };

            var sasBlobToken = blob.GetSharedAccessSignature(sasConstraints);

            return blob.Uri + sasBlobToken;
        }

        private static void CreateSharedAccessPolicy(CloudBlobContainer container, string policyName)
        {
            var permissions = container.GetPermissions();

            var sharedPolicy = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddHours(24),
                Permissions = SharedAccessBlobPermissions.Write | SharedAccessBlobPermissions.List |
                              SharedAccessBlobPermissions.Read
            };

            permissions.SharedAccessPolicies.Add(policyName, sharedPolicy);
            container.SetPermissions(permissions);
        }

        private static string GetContainerSasUriWithPolicy(CloudBlobContainer container, string policyName)
        {
            var sasContainerToken = container.GetSharedAccessSignature(null, policyName);

            return container.Uri + sasContainerToken;
        }

        private static string GetBlobSasUriWithPolicy(CloudBlobContainer container, string policyName)
        {
            var blob = container.GetBlockBlobReference("sasblobpolicy.txt");

            var ms = new MemoryStream(
                Encoding.UTF8.GetBytes("This blob will be accessible to clients via a shared access signature. " +
                                       "A stored access policy defines the constraints for the signature."))
            {
                Position = 0
            };

            using (ms)
            {
                blob.UploadFromStream(ms);
            }

            var sasBlobToken = blob.GetSharedAccessSignature(null, policyName);

            return blob.Uri + sasBlobToken;
        }
    }
}