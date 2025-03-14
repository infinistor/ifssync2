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
using Microsoft.Win32;

namespace IfsSync2Data
{
	public class JobStatus
	{
		#region Attributes
		private const string JOB_VSS = "VSS";
		private const string JOB_FILTER = "Filter";
		private const string JOB_SENDER = "Sender";
		private const string JOB_QUIT = "Quit";
		private const string JOB_STOP = "Stop";
		private const string JOB_ERROR = "Error";

		private const string REMAINING_FILE_COUNT = "RemainingCount";
		private const string REMAINING_FILE_SIZE = "RemainingSize";
		private const string UPLOAD_FILE_COUNT = "UploadCount";
		private const string UPLOAD_FILE_FAIL_COUNT = "UploadFailCount";
		private const string UPLOAD_FILE_SIZE = "UploadSize";

		private const int MY_TRUE = 1;
		private const int MY_FALSE = 0;
		#endregion

		private readonly RegistryKey JobStatusKey = null;
		private readonly string KeyName;
		public readonly string JobName;
		public readonly string HostName;

#pragma warning disable CA1416
		public bool VSS
		{
			get => int.TryParse(JobStatusKey?.GetValue(JOB_VSS)?.ToString(), out int value) && value != MY_FALSE;
			set => JobStatusKey?.SetValue(JOB_VSS, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Filter
		{
			get => int.TryParse(JobStatusKey?.GetValue(JOB_FILTER)?.ToString(), out int value) && value != MY_FALSE;
			set => JobStatusKey?.SetValue(JOB_FILTER, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Sender
		{
			get => int.TryParse(JobStatusKey?.GetValue(JOB_SENDER)?.ToString(), out int value) && value != MY_FALSE;
			set => JobStatusKey?.SetValue(JOB_SENDER, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Quit
		{
			get => int.TryParse(JobStatusKey?.GetValue(JOB_QUIT)?.ToString(), out int value) && value != MY_FALSE;
			set => JobStatusKey?.SetValue(JOB_QUIT, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Stop
		{
			get => int.TryParse(JobStatusKey?.GetValue(JOB_STOP)?.ToString(), out int value) && value != MY_FALSE;
			set => JobStatusKey?.SetValue(JOB_STOP, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Error
		{
			get => int.TryParse(JobStatusKey?.GetValue(JOB_ERROR)?.ToString(), out int value) && value != MY_FALSE;
			set => JobStatusKey?.SetValue(JOB_ERROR, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}

		public string Status
		{
			get
			{
				if (Quit) return "Done";
				if (Error) return "Error";
				if (Stop) return "Stop";
				if (Sender) return "Uploading";
				if (Filter)
				{
					if (KeyName == MainData.INSTANT_BACKUP_NAME) return "Scanning";
					return "Monitoring";
				}
				return "Waiting";
			}
		}

		public long RemainingCount
		{
			get => long.TryParse(JobStatusKey?.GetValue(REMAINING_FILE_COUNT)?.ToString(), out long value) ? value : 0;
			set => JobStatusKey?.SetValue(REMAINING_FILE_COUNT, value, RegistryValueKind.QWord);
		}
		public long RemainingSize
		{
			get => long.TryParse(JobStatusKey?.GetValue(REMAINING_FILE_SIZE)?.ToString(), out long value) ? value : 0;
			set => JobStatusKey?.SetValue(REMAINING_FILE_SIZE, value, RegistryValueKind.QWord);
		}
		public long UploadCount
		{
			get => long.TryParse(JobStatusKey?.GetValue(UPLOAD_FILE_COUNT)?.ToString(), out long value) ? value : 0;
			set => JobStatusKey?.SetValue(UPLOAD_FILE_COUNT, value, RegistryValueKind.QWord);
		}
		public long UploadFailCount
		{
			get => long.TryParse(JobStatusKey?.GetValue(UPLOAD_FILE_FAIL_COUNT)?.ToString(), out long value) ? value : 0;
			set => JobStatusKey?.SetValue(UPLOAD_FILE_FAIL_COUNT, value, RegistryValueKind.QWord);
		}
		public long UploadSize
		{
			get => long.TryParse(JobStatusKey?.GetValue(UPLOAD_FILE_SIZE)?.ToString(), out long value) ? value : 0;
			set => JobStatusKey?.SetValue(UPLOAD_FILE_SIZE, value, RegistryValueKind.QWord);
		}

		public void RenameUpdate(long Count, long Size)
		{
			RemainingCount = Count;
			RemainingSize = Size;
		}
		public void UploadFail()
		{
			UploadFailCount++;
			RemainingCount--;
		}
		public void UploadSuccess(long FileSize = 0)
		{
			UploadCount++;
			RemainingCount--;
			if (FileSize > 0)
			{
				UploadSize += FileSize;
				RemainingSize -= FileSize;
			}
		}

		public JobStatus(string _HostName, string _JobName, bool Write = false)
		{
			JobName = _JobName;
			HostName = _HostName;
			KeyName = MainData.CreateRegistryJobName(HostName, JobName);

			JobStatusKey = Registry.LocalMachine.OpenSubKey(KeyName, Write);
			if (JobStatusKey == null && Write)
			{
				JobStatusKey = Registry.LocalMachine.CreateSubKey(KeyName, true);
				JobStatusKey.SetValue(JOB_VSS, MY_FALSE, RegistryValueKind.DWord);
				JobStatusKey.SetValue(JOB_FILTER, MY_FALSE, RegistryValueKind.DWord);
				JobStatusKey.SetValue(JOB_SENDER, MY_FALSE, RegistryValueKind.DWord);
				JobStatusKey.SetValue(JOB_QUIT, MY_FALSE, RegistryValueKind.DWord);
				JobStatusKey.SetValue(JOB_STOP, MY_FALSE, RegistryValueKind.DWord);
				JobStatusKey.SetValue(JOB_ERROR, MY_FALSE, RegistryValueKind.DWord);

				JobStatusKey.SetValue(REMAINING_FILE_COUNT, 0, RegistryValueKind.QWord);
				JobStatusKey.SetValue(REMAINING_FILE_SIZE, 0, RegistryValueKind.QWord);
				JobStatusKey.SetValue(UPLOAD_FILE_COUNT, 0, RegistryValueKind.QWord);
				JobStatusKey.SetValue(UPLOAD_FILE_FAIL_COUNT, 0, RegistryValueKind.QWord);
				JobStatusKey.SetValue(UPLOAD_FILE_SIZE, 0, RegistryValueKind.QWord);

				if (JobName == MainData.INSTANT_BACKUP_NAME) JobStatusKey.SetValue(JOB_QUIT, MY_TRUE, RegistryValueKind.DWord);
			}
		}
#pragma warning restore CA1416

		public void Clear()
		{
			VSS = false;
			Filter = false;
			Sender = false;
			Error = false;
			UploadClear();
		}
		public void UploadClear()
		{
			RemainingCount = 0;
			RemainingSize = 0;
			UploadCount = 0;
			UploadFailCount = 0;
			UploadSize = 0;
		}
	}
}
