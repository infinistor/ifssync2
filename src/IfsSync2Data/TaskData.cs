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
	public class TaskData
	{
		//Index, FileName, Policy, Path, event time, upload time, upload flag
		public long Index { get; set; }
		public TaskNameList TaskName { get; set; }
		public string FilePath { get; set; }
		public string NewFilePath { get; set; }
		public string SnapshotPath { get; set; }
		public long FileSize { get; set; }
		public string EventTime { get; set; }
		public string UploadTime { get; set; }
		public string Result { get; set; }
		public bool UploadFlag { get; set; }
		public TaskData()
		{
			Init();
		}
		public TaskData(TaskNameList taskName, string filepath, string eventTime)
		{
			Init();
			TaskName = taskName;
			FilePath = filepath;
			EventTime = eventTime;
		}

		public TaskData(TaskNameList taskName, string filepath, string eventTime, long _FileSize)
			: this(taskName, filepath, eventTime) { FileSize = _FileSize; }

		public TaskData(TaskNameList taskName, string filepath, string eventTime, string newFilepath)
			: this(taskName, filepath, eventTime) { NewFilePath = newFilepath; }

		public void Init()
		{
			Index = 0;
			TaskName = TaskNameList.None;
			FilePath = string.Empty;
			NewFilePath = string.Empty;
			SnapshotPath = string.Empty;
			FileSize = 0;
			EventTime = string.Empty;
			UploadTime = string.Empty;
			Result = string.Empty;
			UploadFlag = false;
		}

		public string StrTaskName
		{
			get
			{
				return TaskName.ToString();
			}
			set
			{
				TaskName = TaskNameList.None;
				for (TaskNameList i = TaskNameList.Upload; i <= TaskNameList.Delete; i++)
					if (i.ToString() == value)
						TaskName = i;
			}
		}
	}
}
