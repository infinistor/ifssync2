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

namespace IfsSync2UI
{
	public class ProgressData
	{
		public bool IsQuit { get; set; }
		public bool IsRunning { get; set; }
		public int TotalCount { get; set; }
		public int ZeroCount { get; set; }
		public int CheckCount { get; set; }
		public int UploadFileCount { get; set; }
		public long UploadFileSize { get; set; }

		public ProgressData()
		{
			Init();
		}
		public void Init()
		{
			IsQuit = false;
			IsRunning = false;
			TotalCount = 0;
			CheckCount = 0;
			ZeroCount = 0;
			UploadFileCount = 0;
			UploadFileSize = 0;
		}

		public void Start()
		{
			Init();
			IsRunning = true;

		}
		public void Stop()
		{
			IsRunning = false;
			IsQuit = true;
		}
	}
}
