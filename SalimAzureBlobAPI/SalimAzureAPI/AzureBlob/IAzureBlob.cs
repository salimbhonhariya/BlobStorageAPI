using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobStorage.SalimAzureAPI.AzureBlob
{
        public interface IAzureBlob
        {
            /// <summary>
            /// Uploads the BLOB.
            /// </summary>
            /// <param name="FileToUpload">The file to upload.</param>
            /// <param name="optionsAccessor">The options accessor.</param>
            /// <returns></returns>
            Task<bool> UploadBlob(BlobUpload FileToUpload, IOptions<AzureBlobSettings> optionsAccessor);
            /// <summary>
            /// Downloads the BLOB.
            /// </summary>
            /// <param name="blobName">Name of the BLOB.</param>
            /// <param name="optionsAccessor">The options accessor.</param>
            /// <returns></returns>
            Task<byte[]> DownloadBlob(string blobName, IOptions<AzureBlobSettings> optionsAccessor);
            /// <summary>
            /// Deletes the BLOB.
            /// </summary>
            /// <param name="blobName">Name of the BLOB.</param>
            /// <param name="optionsAccessor">The options accessor.</param>
            /// <returns></returns>
            Task<bool> DeleteBlob(string blobName, IOptions<AzureBlobSettings> optionsAccessor);
        }
}
