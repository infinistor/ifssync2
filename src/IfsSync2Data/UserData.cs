/*
* Copyright (c) 2021 PSPACE, inc. KSAN Development Team ksan@pspace.co.kr
* KSAN is a suite of free software: you can redistribute it and/or modify it under the terms of
* the GNU General Public License as published by the Free Software Foundation, either version 
* 3 of the License.  See LICENSE for details
*
* 본 프로그램 및 관련 소스코드, 문서 등 모든 자료는 있는 그대로 제공이 됩니다.
* KSAN 프로젝트의 개발자 및 개발사는 이 프로그램을 사용한 결과에 따른 어떠한 책임도 지지 않습니다.
* KSAN 개발팀은 사전 공지, 허락, 동의 없이 KSAN 개발에 관련된 모든 결과물에 대한 LICENSE 방식을 변경 할 권리가 있습니다.
*/

namespace IfsSync2Data
{
	public class UserData
	{
		public int Id { get; set; }
		public string HostName { get; set; }
		public string URL { get; set; }
		public string UserName { get; set; }
		public string AccessKey { get; set; }
		public string SecretKey { get; set; }
		public string S3FileManagerURL { get; set; }
		public string StorageName { get; set; }
		public bool Debug { get; set; }
		public bool UpdateFlag { get; set; }
		public UserData()
		{
			Id = 0;
			HostName = string.Empty;
			URL = string.Empty;
			UserName = string.Empty;
			AccessKey = string.Empty;
			SecretKey = string.Empty;
			S3FileManagerURL = string.Empty;
			StorageName = string.Empty;
			Debug = false;
			UpdateFlag = true;
		}

		public UserData(UserData Data)
		{
			CopyTo(Data);
		}

		public void CopyTo(UserData Data)
		{
			Id = Data.Id;
			HostName = Data.HostName;
			URL = Data.URL;
			UserName = Data.UserName;
			AccessKey = Data.AccessKey;
			SecretKey = Data.SecretKey;
			S3FileManagerURL = Data.S3FileManagerURL;
			StorageName = Data.StorageName;
			Debug = Data.Debug;
			UpdateFlag = Data.UpdateFlag;
		}
	}
}
