/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version
* 3 of the License.See LICENSE for details
*
* 본 프로그램 및 관련 소스코드, 문서 등 모든 자료는 있는 그대로 제공이 됩니다.
* KSAN 프로젝트의 개발자 및 개발사는 이 프로그램을 사용한 결과에 따른 어떠한 책임도 지지 않습니다.
* KSAN 개발팀은 사전 공지, 허락, 동의 없이 KSAN 개발에 관련된 모든 결과물에 대한 LICENSE 방식을 변경 할 권리가 있습니다.
*/
using System.IO;

namespace IfsSync2Sender
{
	/// <summary>
	/// CRC64NVMe 클래스
	/// </summary>
	public static class CRC64NVMe
	{
		private const ulong Polynomial = 0x9A6C9329AC4BC9B5L; // NVMe CRC64 다항식
		private static readonly ulong[] Table = new ulong[256];

		static CRC64NVMe()
		{
			for (ulong i = 0; i < 256; i++)
			{
				ulong crc = i;
				for (int j = 0; j < 8; j++)
				{
					if ((crc & 1) != 0)
						crc = (crc >> 1) ^ Polynomial;
					else
						crc >>= 1;
				}
				Table[i] = crc;
			}
		}

		public static ulong Compute(byte[] data)
		{
			ulong crc = 0xFFFFFFFFFFFFFFFF; // 초기값 (AWS 맞추기)
			foreach (byte b in data)
			{
				byte index = (byte)(crc ^ b);
				crc = (crc >> 8) ^ Table[index];
			}
			return crc ^ 0xFFFFFFFFFFFFFFFF; // 최종 XOR
		}

		public static ulong Compute(Stream stream)
		{
			ulong crc = 0xFFFFFFFFFFFFFFFF; // 초기값 (AWS 맞추기)
			byte[] buffer = new byte[4096];
			int bytesRead;
			while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
			{
				for (int i = 0; i < bytesRead; i++)
				{
					byte index = (byte)(crc ^ buffer[i]);
					crc = (crc >> 8) ^ Table[index];
				}
			}
			return crc ^ 0xFFFFFFFFFFFFFFFF; // 최종 XOR
		}
	}
}