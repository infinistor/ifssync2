using System;
using System.IO;
using System.Security.Cryptography;
using System.IO.Hashing;
using System.Numerics;

namespace IfsSync2Sender
{
	static public class ChecksumCalculator
	{
		public static string CalculateChecksum(string filePath, S3ChecksumAlgorithm algorithm)
		{
			if (algorithm == S3ChecksumAlgorithm.None)
				return string.Empty;

			if (filePath.Length > 320)
			{
				return string.Empty;
			}

			if (!File.Exists(filePath))
				throw new FileNotFoundException("파일을 찾을 수 없습니다.", filePath);

			return algorithm switch
			{
				S3ChecksumAlgorithm.CRC32 => CalculateCRC32(filePath),
				S3ChecksumAlgorithm.CRC32C => CalculateCRC32C(filePath),
				S3ChecksumAlgorithm.CRC64NVME => CalculateCRC64NVME(filePath),
				S3ChecksumAlgorithm.SHA1 => CalculateSHA1(filePath),
				S3ChecksumAlgorithm.SHA256 => CalculateSHA256(filePath),
				_ => throw new ArgumentException("지원하지 않는 체크섬 알고리즘입니다.", nameof(algorithm))
			};
		}

		private static string CalculateCRC32(string filePath)
		{
			using var fileStream = File.OpenRead(filePath);
			var crc32 = new Crc32();
			crc32.Append(fileStream);
			var hash = crc32.GetCurrentHash();
			if (BitConverter.IsLittleEndian)
				Array.Reverse(hash);
			return Convert.ToBase64String(hash);
		}

		private static string CalculateCRC32C(string filePath)
		{
			using var fileStream = File.OpenRead(filePath);
			byte[] buffer = new byte[4096];
			uint crc = 0xFFFFFFFF;
			int bytesRead;

			while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
			{
				for (int i = 0; i < bytesRead; i++)
				{
					crc = BitOperations.Crc32C(crc, buffer[i]);
				}
			}

			crc ^= 0xFFFFFFFF;
			var bytes = BitConverter.GetBytes(crc);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return Convert.ToBase64String(bytes);
		}

		private static string CalculateCRC64NVME(string filePath)
		{
			using var fileStream = File.OpenRead(filePath);
			ulong nvmeCrc64 = CRC64NVMe.Compute(fileStream);
			var bytes = BitConverter.GetBytes(nvmeCrc64);
			if (BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			return Convert.ToBase64String(bytes);
		}

		private static string CalculateSHA1(string filePath)
		{
			using var sha1 = SHA1.Create();
			using var fileStream = File.OpenRead(filePath);
			var hash = sha1.ComputeHash(fileStream);
			return Convert.ToBase64String(hash);
		}

		private static string CalculateSHA256(string filePath)
		{
			using var sha256 = SHA256.Create();
			using var fileStream = File.OpenRead(filePath);
			var hash = sha256.ComputeHash(fileStream);
			return Convert.ToBase64String(hash);
		}
	}
}
