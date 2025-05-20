/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version 
* 3 of the License. See LICENSE for details
*
* 본 프로그램 및 관련 소스코드, 문서 등 모든 자료는 있는 그대로 제공이 됩니다.
* KSAN 프로젝트의 개발자 및 개발사는 이 프로그램을 사용한 결과에 따른 어떠한 책임도 지지 않습니다.
* KSAN 개발팀은 사전 공지, 허락, 동의 없이 KSAN 개발에 관련된 모든 결과물에 대한 LICENSE 방식을 변경 할 권리가 있습니다.
*/
namespace IfsSync2Common
{
	/// <summary>
	/// 용량 단위와 관련된 상수 및 변환 유틸리티를 제공하는 클래스
	/// </summary>
	public static class CapacityUnit
	{
		/// <summary>바이트 (1)</summary>
		public const long B = 1L;
		
		/// <summary>킬로바이트 - KB (1,024 바이트)</summary>
		public const long KB = 1024L * B;
		
		/// <summary>메가바이트 - MB (1,024 킬로바이트)</summary>
		public const long MB = 1024L * KB;
		
		/// <summary>기가바이트 - GB (1,024 메가바이트)</summary>
		public const long GB = 1024L * MB;
		
		/// <summary>테라바이트 - TB (1,024 기가바이트)</summary>
		public const long TB = 1024L * GB;
		
		/// <summary>페타바이트 - PB (1,024 테라바이트)</summary>
		public const long PB = 1024L * TB;
		
		/// <summary>엑사바이트 - EB (1,024 페타바이트)</summary>
		public const long EB = 1024L * PB;
		
		/// <summary>
		/// 지정된 인덱스에 해당하는 단위의 문자열 표현을 반환합니다.
		/// 컴퓨터 분야에서는 일반적으로 1024의 배수를 KB, MB 등으로 표기합니다.
		/// </summary>
		/// <param name="index">단위 인덱스 (0=B, 1=KB, 2=MB 등)</param>
		/// <returns>단위 문자열</returns>
		public static string GetUnitString(int index)
		{
			return index switch
			{
				0 => "B",
				1 => "KB",
				2 => "MB",
				3 => "GB",
				4 => "TB",
				5 => "PB",
				6 => "EB",
				7 => "ZB",
				_ => "YB"
			};
		}
		
		/// <summary>
		/// 크기 값을 사람이 읽기 쉬운 문자열로 변환합니다.
		/// </summary>
		/// <param name="value">바이트 단위 크기</param>
		/// <returns>형식화된 크기 문자열</returns>
		public static string Format(long value)
		{
			const float IECPrefix = 1024.0F;
			const float MaxValue = 1000.0f;

			int unitCount = 0;

			float size = value;
			while (size > MaxValue)
			{
				size /= IECPrefix;
				unitCount++;
			}

			return $"{size:0}{GetUnitString(unitCount)}";
		}

		/// <summary>
		/// 크기 문자열을 바이트 단위 값으로 변환합니다.
		/// </summary>
		/// <param name="size">크기 문자열</param>
		/// <returns>바이트 단위 크기</returns>
		public static long Parse(string size)
		{
			if (string.IsNullOrWhiteSpace(size)) return 0;

			size = size.Trim().ToUpper();
			return size switch
			{
				var s when s.EndsWith("TB") => ParseSize(s, "TB", TB),
				var s when s.EndsWith("GB") => ParseSize(s, "GB", GB),
				var s when s.EndsWith("MB") => ParseSize(s, "MB", MB),
				var s when s.EndsWith("KB") => ParseSize(s, "KB", KB),
				_ => ParseSize(size, "", B)
			};
		}

		/// <summary>
		/// 크기 문자열을 파싱합니다.
		/// </summary>
		/// <param name="size">크기 문자열</param>
		/// <param name="unit">단위 문자열</param>
		/// <param name="multiplier">곱셈 인자</param>
		/// <returns>변환된 크기 값</returns>
		private static long ParseSize(string size, string unit, long multiplier)
		{
			return long.TryParse(size.Replace(unit, ""), out long value) ? value * multiplier : 0;
		}
	}
} 