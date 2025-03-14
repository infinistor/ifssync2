using Amazon.S3;
using Amazon.S3.Model;

namespace IfsSync2Sender
{
	public class S3Metadata
	{
		public string ObjectName { get; set; }
		public string MD5 { get; set; }
		public long Size { get; set; }
		public ChecksumType ChecksumType { get; set; }
		public S3ChecksumAlgorithm ChecksumAlgorithm { get; set; }
		public string Checksum { get; set; }

		public S3Metadata(string key, GetObjectMetadataResponse response)
		{
			ObjectName = key;
			MD5 = response.ETag.Replace("\"", string.Empty);
			Size = response.ContentLength;
			ChecksumType = response.ChecksumType;

			if (!string.IsNullOrWhiteSpace(response.ChecksumCRC32))
			{
				ChecksumAlgorithm = S3ChecksumAlgorithm.CRC32;
				Checksum = response.ChecksumCRC32;
			}
			else if (!string.IsNullOrWhiteSpace(response.ChecksumCRC32C))
			{
				ChecksumAlgorithm = S3ChecksumAlgorithm.CRC32C;
				Checksum = response.ChecksumCRC32C;
			}
			else if (!string.IsNullOrWhiteSpace(response.ChecksumCRC64NVME))
			{
				ChecksumAlgorithm = S3ChecksumAlgorithm.CRC64NVME;
				Checksum = response.ChecksumCRC64NVME;
			}
			else if (!string.IsNullOrWhiteSpace(response.ChecksumSHA1))
			{
				ChecksumAlgorithm = S3ChecksumAlgorithm.SHA1;
				Checksum = response.ChecksumSHA1;
			}
			else if (!string.IsNullOrWhiteSpace(response.ChecksumSHA256))
			{
				ChecksumAlgorithm = S3ChecksumAlgorithm.SHA256;
				Checksum = response.ChecksumSHA256;
			}
			else
			{
				ChecksumAlgorithm = S3ChecksumAlgorithm.None;
				Checksum = string.Empty;
			}
		}
	}
}
