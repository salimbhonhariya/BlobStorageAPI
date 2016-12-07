using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobStorage.SalimAzureAPI.AzureBlob
{
    class BlobUpload
    {
        //[JsonProperty("filename")]
        //public string FileName { get; set; }
        [JsonProperty("fileurl")]
        public string FileUrl { get; set; }
        [JsonProperty("filesizeinbytes")]
        public long FileSizeInBytes { get; set; }
        [JsonProperty("filesizeinkb")]
        public long FileSizeInKb { get { return (long)Math.Ceiling((double)FileSizeInBytes / 1024); } }
    }
}
