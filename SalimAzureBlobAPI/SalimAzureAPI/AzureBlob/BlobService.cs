using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;

namespace BlobStorage
{
    interface BlobService:IAzureBlob
    {
        private CloudBlobClient _blobClient;
        private CloudBlobContainer _blobContainer;
        private CloudBlockBlob _blockBlob;
        private string _blobContainerName;
        private byte[] _downloadResult;
        private string _blobFileDownloadLocation;

        /// <summary>
        /// Uploads the BLOB.
        /// </summary>
        /// <param name="FileToUpload">The file to upload.</param>
        /// <param name="azureAppSettings">The azure application settings.</param>
        /// <returns></returns>
        public async Task<bool> UploadBlob(BlobUpload FileToUpload, IOptions<AzureBlobSettings> azureAppSettings)
        {
            try
            {
                GetAzureBlobStorageAccountContainer(azureAppSettings);

                // Create a container for organizing blobs within the storage account.
                _blobContainer = _blobClient.GetContainerReference(_blobContainerName);

                await _blobContainer.CreateIfNotExistsAsync();

                // To view the uploaded blob in a browser, you have two options. The first option is to use a Shared Access Signature (SAS) token to delegate  
                // access to the resource. See the documentation links at the top for more information on SAS. The second approach is to set permissions  
                // to allow public access to blobs in this container. Comment the line below to not use this approach and to use SAS. Then you can view the image  
                // using: https://[InsertYourStorageAccountNameHere].blob.core.windows.net/webappstoragedotnet-imagecontainer/FileName 
                await _blobContainer.SetPermissionsAsync(new BlobContainerPermissions { PublicAccess = BlobContainerPublicAccessType.Blob });

                var justFileName = Path.GetFileName(FileToUpload.FileUrl);

                // Upload a BlockBlob to the newly created container
                CloudBlockBlob blockBlob = _blobContainer.GetBlockBlobReference(justFileName);

                await blockBlob.UploadFromFileAsync(FileToUpload.FileUrl);

                // get the url of the newly created Blob IN AZURE
                var NewFileURL = blockBlob.Uri.AbsoluteUri;

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Download the BLOB.
        /// </summary>
        /// <param name="blobName">Name of the BLOB.</param>
        /// <param name="azureAppSettings">The azure application settings.</param>
        /// <returns></returns>
        public async Task<byte[]> DownloadBlob(string blobName, IOptions<AzureBlobSettings> azureAppSettings)
        {
            try
            {
                GetAzureBlobStorageAccountContainer(azureAppSettings);

                CloudBlobContainer container = _blobClient.GetContainerReference(_blobContainerName);

                _blockBlob = container.GetBlockBlobReference(blobName);

                await _blockBlob.FetchAttributesAsync();

                _blobFileDownloadLocation = _blobFileDownloadLocation + blobName;
                // Save blob contents to a file.
                //using (var fileStream = new System.IO.FileStream(@"C:\\Users\\sbhonhariya\\Documents\\a.docx", FileMode.Create))
                using (var fileStream = new System.IO.FileStream(@" " + _blobFileDownloadLocation, FileMode.Create))
                {
                    await _blockBlob.DownloadToStreamAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                return null;
            }
            return _downloadResult;
        }

        /// <summary>
        /// Gets the azure BLOB storage account container.
        /// </summary>
        /// <param name="azureAppSettings">The azure application settings.</param>
        private void GetAzureBlobStorageAccountContainer(IOptions<AzureBlobSettings> azureAppSettings)
        {
            // Validates the connection string information in app.config and throws an exception if it looks like 
            AzureBlobSettings appSettings = azureAppSettings.Value;

            _blobContainerName = appSettings.BlobContainerName;

            // Retrieve storage account information from connection string
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(appSettings.AzureBlobConnectionString);

            // Create a blob client for interacting with the blob service.
            _blobClient = storageAccount.CreateCloudBlobClient();

            //get filedownload location
            _blobFileDownloadLocation = appSettings.BlobFileDownloadLocation;
        }


        /// <summary>
        /// Deletes the specified blob from the specified container if it exists.
        /// </summary>
        /// <param name="blobName">The blob "file" to delete.</param>
        /// <param name="azureAppSettings">The azure application settings.</param>
        /// <returns></returns>
        public async Task<bool> DeleteBlob(string blobName, IOptions<AzureBlobSettings> azureAppSettings)
        {
            try
            {
                GetAzureBlobStorageAccountContainer(azureAppSettings);

                CloudBlobContainer container = _blobClient.GetContainerReference(_blobContainerName);
                _blockBlob = container.GetBlockBlobReference(blobName);
                await _blockBlob.DeleteAsync();
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

    }
}
