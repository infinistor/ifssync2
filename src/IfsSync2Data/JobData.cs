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
using System.Collections.ObjectModel;
using System.Linq;

namespace IfsSync2Data
{
	/// <summary> 작업 데이터 클래스 </summary>
	public sealed class JobData : IEquatable<JobData>
	{
		private const char Separator = '|';
		/// <summary> 작업 정책 목록 </summary>
		public enum PolicyType
		{
			/// <summary>Start immediately</summary>
			Now = -1,
			/// <summary>Real time upload</summary>
			RealTime = 0,
			/// <summary>Every few hours</summary>
			Schedule,
			/// <summary>Instant backup</summary>
			Instant
		}
		public int Id { get; set; }
		public bool Global { get; set; }
		public string HostName { get; set; }
		public string JobName { get; set; }
		public bool IsGlobalUser { get; set; }
		public int UserId { get; set; }
		public ObservableCollection<string> Path { get; set; }
		public ObservableCollection<string> BlackPath { get; set; }
		public ObservableCollection<string> BlackFile { get; set; }
		public ObservableCollection<string> BlackFileExt { get; set; }
		public ObservableCollection<string> WhiteFile { get; set; }
		public ObservableCollection<string> WhiteFileExt { get; set; }
		public ObservableCollection<string> VSSFileExt { get; set; }
		public ObservableCollection<Schedule> ScheduleList { get; set; }

		public PolicyType Policy { get; set; }
		public bool Remove { get; set; }
		public bool IsInit { get; set; }
		public bool FilterUpdate { get; set; }
		public bool SenderUpdate { get; set; }
		public bool DeleteFlag { get; set; }

		public bool IsInstantBackup => Policy == PolicyType.Instant;

		public JobData()
		{
			Init();
		}

		public void Init()
		{
			Id = -1;
			Global = false;
			HostName = MainData.DEFAULT_HOSTNAME_NAME;
			JobName = MainData.DEFAULT_JOB_NAME;
			IsGlobalUser = true;
			UserId = -1;
			Path = [];
			BlackPath = [];
			BlackFile = [];
			BlackFileExt = [];
			WhiteFile = [];
			WhiteFileExt = [];
			VSSFileExt = [];
			ScheduleList = [];
			Policy = PolicyType.RealTime;
			Remove = true;
			FilterUpdate = false;
			SenderUpdate = false;
			DeleteFlag = false;
		}

		public string StrPath
		{
			get => string.Join(Separator, Path.Select(Dir => Dir.Trim()));
			set
			{
				string[] result = value.Split(Separator);

				Path.Clear();
				foreach (var item in result)
				{
					if (string.IsNullOrWhiteSpace(item)) continue;
					Path.Add(item.Trim());
				}
			}
		}
		public string StrBlackPath
		{
			get => string.Join(Separator, BlackPath.Select(Dir => Dir.Trim()));
			set
			{
				string[] result = value.Split(Separator);

				BlackPath.Clear();
				foreach (var item in result)
				{
					if (string.IsNullOrWhiteSpace(item)) continue;
					BlackPath.Add(item.Trim());
				}
			}
		}
		public string StrBlackFile
		{
			get => string.Join(Separator, BlackFile.Select(Dir => Dir.Trim()));
			set
			{
				string[] result = value.Split(Separator);

				BlackFile.Clear();
				foreach (var item in result)
				{
					if (string.IsNullOrWhiteSpace(item)) continue;
					BlackFile.Add(item.Trim());
				}
			}
		}
		public string StrBlackFileExt
		{
			get => string.Join(Separator, BlackFileExt.Select(Dir => Dir.Trim()));
			set
			{
				string[] result = value.Split(Separator);

				BlackFileExt.Clear();
				foreach (var item in result)
				{
					if (string.IsNullOrWhiteSpace(item)) continue;
					BlackFileExt.Add(item.Trim());
				}
			}
		}
		public string StrWhiteFile
		{
			get => string.Join(Separator, WhiteFile.Select(Dir => Dir.Trim()));
			set
			{
				string[] result = value.Split(Separator);

				WhiteFile.Clear();
				foreach (var item in result)
				{
					if (string.IsNullOrWhiteSpace(item)) continue;
					WhiteFile.Add(item.Trim());
				}
			}
		}
		public string StrWhiteFileExt
		{
			get => string.Join(Separator, WhiteFileExt.Select(Dir => Dir.Trim()));
			set
			{
				string[] result = value.Split(Separator);

				WhiteFileExt.Clear();
				foreach (var item in result)
				{
					if (string.IsNullOrWhiteSpace(item)) continue;
					WhiteFileExt.Add(item.Trim());
				}
			}
		}
		public string StrVSSFileExt
		{
			get => string.Join(Separator, VSSFileExt.Select(Dir => Dir.Trim()));
			set
			{
				string[] result = value.Split(Separator);

				VSSFileExt.Clear();
				foreach (var item in result)
				{
					if (string.IsNullOrWhiteSpace(item)) continue;
					VSSFileExt.Add(item.Trim());
				}
			}
		}
		public string StrPolicy
		{
			set
			{
				for (PolicyType i = PolicyType.Now; i <= PolicyType.Instant; i++)
					if (value.Equals(i.ToString())) { Policy = i; break; }
			}
			get
			{
				return Policy.ToString();
			}
		}

		public bool ExistsDirAndExt(string FilePath)
		{
			string[] result = FilePath.Split('.');
			string Extension = result[^1];

			foreach (string Directory in Path)
			{
				//Directory matches!
				if (FilePath.StartsWith(Directory))
				{
					foreach (var ext in WhiteFileExt)
					{
						//Extension matches
						if (Extension == ext) return true;
					}
				}
			}
			return false;
		}
		public bool ExistsDirectory(string DirectoryPath)
		{
			foreach (string Directory in Path)
			{
				if (Directory == DirectoryPath) return true;
				if (DirectoryPath.StartsWith(Directory)) return true;
			}

			return false;
		}
		public bool ExistsVssExt(string FilePath)
		{
			foreach (string Ext in VSSFileExt)
			{
				if (FilePath.EndsWith(Ext, StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}
		public bool DeleteDirectory(string DirectoryPath)
		{
			for (int index = 0; index < Path.Count; index++)
			{
				if (Path[index].Equals(DirectoryPath))
				{
					Path.RemoveAt(index);
					return true;
				}
			}
			return false;
		}

		public bool CheckToSchedules()
		{
			if (Policy != PolicyType.Schedule) return true;

			DateTime time = DateTime.Now;
			string Week = time.DayOfWeek.ToString();
			int Hours = time.Hour;
			int Mins = time.Minute;

			foreach (Schedule item in ScheduleList)
			{
				if (item.IsExistSchedule(Week, Hours, Mins)) return true;
			}
			return false;
		}

		public bool Equals(JobData item)
		{
			if (item == null) return false;

			if (Id != item.Id) return false;
			if (Global != item.Global) return false;
			if (HostName != item.HostName) return false;
			if (IsGlobalUser != item.IsGlobalUser) return false;
			if (UserId != item.UserId) return false;
			if (StrPath != item.StrPath) return false;
			if (StrBlackPath != item.StrBlackPath) return false;
			if (StrBlackFile != item.StrBlackFile) return false;
			if (StrBlackFileExt != item.StrBlackFileExt) return false;
			if (StrWhiteFile != item.StrWhiteFile) return false;
			if (StrWhiteFileExt != item.StrWhiteFileExt) return false;
			if (Policy != item.Policy) return false;
			if (Remove != item.Remove) return false;
			if (ScheduleList.Count != item.ScheduleList.Count) return false;
			for (int i = 0; i < ScheduleList.Count; i++) if (ScheduleList[i].Equals(item.ScheduleList[i])) return false;

			return true;
		}
		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			var item = obj as JobData;

			return Equals(item);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Global, HostName, IsGlobalUser, UserId, StrPath, Policy, Remove);
		}

		public void CopyTo(JobData Data)
		{
			HostName = Data.HostName;
			JobName = Data.JobName;
			Policy = Data.Policy;
			IsInit = Data.IsInit;

			CopyToList(Data);
		}
		public void CopyToList(JobData Data)
		{
			Path = [.. Data.Path];
			BlackPath = [.. Data.BlackPath];
			BlackFile = [.. Data.BlackFile];
			BlackFileExt = [.. Data.BlackFileExt];
			WhiteFile = [.. Data.WhiteFile];
			WhiteFileExt = [.. Data.WhiteFileExt];
		}
	}

}
