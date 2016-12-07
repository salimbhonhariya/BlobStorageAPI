using System;
using System.Threading.Tasks;

namespace BlobStorage
{
    //[Authorize]
    //[ApiExplorerSettings(IgnoreApi = true)]
    //[Produces("application/json")]
    //[Route("api/")]

    public class AzureBlobController : Controller
    {
        private IOptions<AzureBlobSettings> _azureBlobSettings;
        private IAzureBlob _blobService = new BlobService();

        public AzureBlobController(IOptions<AzureBlobSettings> azureSettings)
        {
            _azureBlobSettings = azureSettings;
        }

        /// <summary>
        /// Uploads one or more blob files.
        /// </summary>
        /// <returns></returns>
        [HttpPost("[controller]/upload")]
        public async Task<IActionResult> PostBlobUpload([FromBody]BlobUpload obj)
        {
            try
            {
                // Call service to perform upload, then check result to return as content
                var result = await _blobService.UploadBlob(obj, _azureBlobSettings);

                if (result == true)
                {
                    return Ok(result);
                }

                // Otherwise
                return BadRequest();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        /// <summary>
        /// Downloas one file
        /// </summary>
        /// <param name="filetoDownload">The file to download.</param>
        /// <returns></returns>
        [HttpGet("[controller]/GetBlobDownload/{filetoDownload}")]
        public async Task<IActionResult> GetBlobDownload([FromRoute]string filetoDownload)
        {
            try
            {
                // Call service to perform upload, then check result to return as content
                var result = await _blobService.DownloadBlob(filetoDownload, _azureBlobSettings);

                if (result != null)
                {
                    return Ok(result);
                }

                // Otherwise
                return BadRequest();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Deletes the BLOB.
        /// </summary>
        /// <param name="filetoDelete">The fileto delete.</param>
        /// <returns></returns>
        [HttpDelete("[controller]/delete/{filetoDelete}")]
        public async Task<IActionResult> DeleteBlob([FromRoute]string filetoDelete)
        {
            try
            {
                // Call service to perform upload, then check result to return as content
                var result = await _blobService.DeleteBlob(filetoDelete, _azureBlobSettings);

                if (result == true)
                {
                    return Ok(result);
                }

                // Otherwise
                return BadRequest();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        private IActionResult InternalServerError(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
