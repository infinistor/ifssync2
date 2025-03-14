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

namespace IfsSync2Data
{
	public class TaskData
	{
		public enum TaskTypeList { None = -1, Upload = 0, Rename, Delete }
		//Index, FileName, Policy, Path, event time, upload time, upload flag
		public long Index { get; set; }
		public TaskTypeList TaskType { get; set; }
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
		public TaskData(TaskTypeList taskType, string filepath, string eventTime)
		{
			Init();
			TaskType = taskType;
			FilePath = filepath;
			EventTime = eventTime;
		}

		public TaskData(TaskTypeList taskType, string filepath, string eventTime, long _FileSize)
			: this(taskType, filepath, eventTime) { FileSize = _FileSize; }

		public TaskData(TaskTypeList taskType, string filepath, string eventTime, string newFilepath)
			: this(taskType, filepath, eventTime) { NewFilePath = newFilepath; }

		public void Init()
		{
			Index = 0;
			TaskType = TaskTypeList.None;
			FilePath = string.Empty;
			NewFilePath = string.Empty;
			SnapshotPath = string.Empty;
			FileSize = 0;
			EventTime = string.Empty;
			UploadTime = string.Empty;
			Result = string.Empty;
			UploadFlag = false;
		}

		public string StrTaskType
		{
			get => TaskType.ToString();
			set => TaskType = Enum.TryParse<TaskTypeList>(value, out var result) ? result : TaskTypeList.None;
		}
	}
}
