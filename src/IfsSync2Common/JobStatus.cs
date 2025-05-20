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

namespace IfsSync2Common
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

		private readonly RegistryKey _jobStatusKey;
		private readonly string KeyName;
		public readonly string JobName;
		public readonly string HostName;

#pragma warning disable CA1416
		public bool VSS
		{
			get => RegistryUtility.GetBoolValue(_jobStatusKey, JOB_VSS);
			set => _jobStatusKey.SetValue(JOB_VSS, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Filter
		{
			get => RegistryUtility.GetBoolValue(_jobStatusKey, JOB_FILTER);
			set => _jobStatusKey.SetValue(JOB_FILTER, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Sender
		{
			get => RegistryUtility.GetBoolValue(_jobStatusKey, JOB_SENDER);
			set => _jobStatusKey.SetValue(JOB_SENDER, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Quit
		{
			get => RegistryUtility.GetBoolValue(_jobStatusKey, JOB_QUIT);
			set => _jobStatusKey.SetValue(JOB_QUIT, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Stop
		{
			get => RegistryUtility.GetBoolValue(_jobStatusKey, JOB_STOP);
			set => _jobStatusKey.SetValue(JOB_STOP, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Error
		{
			get => RegistryUtility.GetBoolValue(_jobStatusKey, JOB_ERROR);
			set => _jobStatusKey.SetValue(JOB_ERROR, value ? MY_TRUE : MY_FALSE, RegistryValueKind.DWord);
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
					if (KeyName == IfsSync2Constants.INSTANT_BACKUP_NAME) return "Scanning";
					return "Monitoring";
				}
				return "Waiting";
			}
		}

		public long RemainingCount
		{
			get => RegistryUtility.GetLongValue(_jobStatusKey, REMAINING_FILE_COUNT);
			set => _jobStatusKey.SetValue(REMAINING_FILE_COUNT, value, RegistryValueKind.QWord);
		}
		public long RemainingSize
		{
			get => RegistryUtility.GetLongValue(_jobStatusKey, REMAINING_FILE_SIZE);
			set => _jobStatusKey.SetValue(REMAINING_FILE_SIZE, value, RegistryValueKind.QWord);
		}
		public long UploadCount
		{
			get => RegistryUtility.GetLongValue(_jobStatusKey, UPLOAD_FILE_COUNT);
			set => _jobStatusKey.SetValue(UPLOAD_FILE_COUNT, value, RegistryValueKind.QWord);
		}
		public long UploadFailCount
		{
			get => RegistryUtility.GetLongValue(_jobStatusKey, UPLOAD_FILE_FAIL_COUNT);
			set => _jobStatusKey.SetValue(UPLOAD_FILE_FAIL_COUNT, value, RegistryValueKind.QWord);
		}
		public long UploadSize
		{
			get => RegistryUtility.GetLongValue(_jobStatusKey, UPLOAD_FILE_SIZE);
			set => _jobStatusKey.SetValue(UPLOAD_FILE_SIZE, value, RegistryValueKind.QWord);
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
			KeyName = IfsSync2Utilities.CreateRegistryJobName(HostName, JobName);

			var temp = Registry.LocalMachine.OpenSubKey(KeyName, Write);
			if (temp == null)
			{
				temp = Registry.LocalMachine.CreateSubKey(KeyName, true);
				temp.SetValue(JOB_VSS, MY_FALSE, RegistryValueKind.DWord);
				temp.SetValue(JOB_FILTER, MY_FALSE, RegistryValueKind.DWord);
				temp.SetValue(JOB_SENDER, MY_FALSE, RegistryValueKind.DWord);
				temp.SetValue(JOB_QUIT, MY_FALSE, RegistryValueKind.DWord);
				temp.SetValue(JOB_STOP, MY_FALSE, RegistryValueKind.DWord);
				temp.SetValue(JOB_ERROR, MY_FALSE, RegistryValueKind.DWord);

				temp.SetValue(REMAINING_FILE_COUNT, 0, RegistryValueKind.QWord);
				temp.SetValue(REMAINING_FILE_SIZE, 0, RegistryValueKind.QWord);
				temp.SetValue(UPLOAD_FILE_COUNT, 0, RegistryValueKind.QWord);
				temp.SetValue(UPLOAD_FILE_FAIL_COUNT, 0, RegistryValueKind.QWord);
				temp.SetValue(UPLOAD_FILE_SIZE, 0, RegistryValueKind.QWord);

				if (JobName == IfsSync2Constants.INSTANT_BACKUP_NAME) temp.SetValue(JOB_QUIT, MY_TRUE, RegistryValueKind.DWord);
			}
			_jobStatusKey = temp;
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