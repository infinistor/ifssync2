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
using System;
using IfsSync2Data;

namespace IfsSync2UI
{
	public class StorageData
	{
		public readonly int Id;
		public readonly string HostName;
		public readonly string UserName;

		public string AccessKey { get; set; }
		public string AccessSecret { get; set; }
		public string S3FileManagerURL { get; set; }

		public bool Delete { get; set; }
		public string StorageName { get; set; }
		public string URL { get; set; }
		public bool IsGlobalUser => string.IsNullOrWhiteSpace(HostName);
		public long TotalSize { get; set; }
		public long UsedSize { get; set; }

		public bool IsS3 => URL.StartsWith(MainData.HTTP, StringComparison.OrdinalIgnoreCase) || URL.StartsWith(MainData.HTTPS, StringComparison.OrdinalIgnoreCase);
		public long FreeSize => TotalSize - UsedSize;
		public double Rate => TotalSize == 0 ? 0 : UsedSize / (double)TotalSize * 100.0;

		public string StrTotalSize => MainData.SizeToString(TotalSize);
		public string StrUsedSize => MainData.SizeToString(UsedSize);
		public string StrFreeSize => MainData.SizeToString(FreeSize);
		public string StrRate => TotalSize == 0 ? "No Data" : string.Format("{0:0.0} %", Rate);

		public StorageData(int id, string hostName, string userName)
		{
			UserName = userName;
			Id = id;
			HostName = hostName;
			Delete = false;
		}
	}
}
