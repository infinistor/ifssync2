using System;
using System.IO;
using System.Security.Cryptography;

namespace IfsSync2Sender
{
	static public class ChecksumCalculator
	{
		private const int BUFFER_SIZE = 81920; // 80KB buffer

		public static string CalculateChecksum(string filePath, S3ChecksumAlgorithm algorithm)
		{
			if (algorithm == S3ChecksumAlgorithm.None)
				return string.Empty;

			if (!File.Exists(filePath))
				throw new FileNotFoundException("파일을 찾을 수 없습니다.", filePath);

			return algorithm switch
			{
				S3ChecksumAlgorithm.CRC32 => CalculateCRC32(filePath),
				S3ChecksumAlgorithm.CRC32C => CalculateCRC32C(filePath),
				S3ChecksumAlgorithm.SHA1 => CalculateSHA1(filePath),
				S3ChecksumAlgorithm.SHA256 => CalculateSHA256(filePath),
				 _ => throw new ArgumentException("지원하지 않는 체크섬 알고리즘입니다.", nameof(algorithm))
			};
		}

		private static string CalculateCRC32(string filePath)
		{
			using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			uint crc32 = 0xFFFFFFFF;
			byte[] buffer = new byte[BUFFER_SIZE];
			int bytesRead;

			while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
			{
				for (int i = 0; i < bytesRead; i++)
				{
					crc32 = (crc32 >> 8) ^ Crc32Table[(crc32 ^ buffer[i]) & 0xFF];
				}
			}

			crc32 ^= 0xFFFFFFFF;
			return crc32.ToString("x8"); // 8자리 16진수
		}

		private static string CalculateCRC32C(string filePath)
		{
			using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			uint crc32c = 0xFFFFFFFF;
			byte[] buffer = new byte[BUFFER_SIZE];
			int bytesRead;

			while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
			{
				for (int i = 0; i < bytesRead; i++)
				{
					crc32c = (crc32c >> 8) ^ Crc32CTable[(crc32c ^ buffer[i]) & 0xFF];
				}
			}

			crc32c ^= 0xFFFFFFFF;
			return crc32c.ToString("x8"); // 8자리 16진수
		}

		private static string CalculateSHA1(string filePath)
		{
			using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			using var sha1 = SHA1.Create();
			byte[] hash = sha1.ComputeHash(fs);
			return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
		}

		private static string CalculateSHA256(string filePath)
		{
			using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			using var sha256 = SHA256.Create();
			byte[] hash = sha256.ComputeHash(fs);
			return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
		}

		private static string CalculateCRC64(string filePath)
		{
			using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
			ulong crc64 = 0xFFFFFFFFFFFFFFFF;
			byte[] buffer = new byte[BUFFER_SIZE];
			int bytesRead;

			while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
			{
				for (int i = 0; i < bytesRead; i++)
				{
					crc64 = (crc64 >> 8) ^ Crc64Table[(crc64 ^ buffer[i]) & 0xFF];
				}
			}

			crc64 ^= 0xFFFFFFFFFFFFFFFF;
			return crc64.ToString("x16"); // 16자리 16진수
		}

		// CRC32 테이블
		private static readonly uint[] Crc32Table = {
			0x00000000, 0x77073096, 0xEE0E612C, 0x990951BA, 0x076DC419, 0x706AF48F, 0xE963A535, 0x9E6495A3,
			0x0EDB8832, 0x79DCB8A4, 0xE0D5E91E, 0x97D2D988, 0x09B64C2B, 0x7EB17CBD, 0xE7B82D07, 0x90BF1D91,
			// ... (나머지 테이블 값들)
		};

		// CRC32C 테이블
		private static readonly uint[] Crc32CTable = {
			0x00000000, 0xF26B8303, 0xE13B70F7, 0x1350F3F4, 0xC79A971F, 0x35F1141C, 0x26A1E7E8, 0xD4CA64EB,
			0x8AD958CF, 0x78B2DBCC, 0x6BE22838, 0x9989AB3B, 0x4D43CFD0, 0xBF284CD3, 0xAC78BF27, 0x5E133C24,
			// ... (나머지 테이블 값들)
		};

		// CRC64 테이블 (ECMA 182)
		private static readonly ulong[] Crc64Table = {
			0x0000000000000000, 0x42F0E1EBA9EA3693, 0x85E1C3D753D46D26, 0xC711223CFA3E5BB5,
			0x493366450E42ECDF, 0x0BC387AEA7A8DA4C, 0xCCD2A5925D9681F9, 0x8E4A28BE4F464A6A,
			// ... (나머지 테이블 값들)
		};
	}
}
