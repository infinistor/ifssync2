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
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace IfsSync2Data
{
	/// <summary> 작업 데이터 클래스 </summary>
	public class JobData : IEquatable<JobData>, IEqualityComparer<JobData>
	{
		private const char Separator = '|';
		/// <summary> 작업 정책 목록 </summary>
		public enum PolicyName
		{
			/// <summary>Start immediately</summary>
			Now = -1,
			/// <summary>Real time upload</summary>
			RealTime = 0,
			/// <summary>Every few hours</summary>
			Schedule,
		}
		public int Id { get; set; }
		public string HostName { get; set; }
		public string JobName { get; set; }
		public bool IsGlobalUser { get; set; }
		public int UserID { get; set; }
		public ObservableCollection<string> Path { get; set; }
		public ObservableCollection<string> BlackPath { get; set; }
		public ObservableCollection<string> BlackFile { get; set; }
		public ObservableCollection<string> BlackFileExt { get; set; }
		public ObservableCollection<string> WhiteFile { get; set; }
		public ObservableCollection<string> WhiteFileExt { get; set; }
		public ObservableCollection<string> VSSFileExt { get; set; }
		public ObservableCollection<Schedule> ScheduleList { get; set; }

		public PolicyName Policy { get; set; }
		public bool Global { get; set; }
		public bool Remove { get; set; }
		public bool IsInit { get; set; }
		public bool FilterUpdate { get; set; }
		public bool SenderUpdate { get; set; }
		public bool DeleteFlag { get; set; }

		public JobData()
		{
			Init();
		}

		public void Init()
		{
			Id = -1;
			HostName = MainData.DEFAULT_HOSTNAME_NAME;
			JobName = MainData.DEFAULT_JOB_NAME;
			IsGlobalUser = true;
			UserID = -1;
			Path = [];
			BlackPath = [];
			BlackFile = [];
			BlackFileExt = [];
			WhiteFile = [];
			WhiteFileExt = [];
			VSSFileExt = [];
			ScheduleList = [];
			Policy = PolicyName.RealTime;
			Global = true;
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
				for (PolicyName i = PolicyName.Now; i <= PolicyName.Schedule; i++)
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
			if (Policy != PolicyName.Schedule) return true;

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
		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			var item = obj as JobData;

			return Equals(item);
		}

		public bool Equals(JobData item)
		{
			if (item == null) return false;

			if (HostName != item.HostName) return false;
			if (IsGlobalUser != item.IsGlobalUser) return false;
			if (UserID != item.UserID) return false;
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

		public void CopyTo(JobData Data)
		{
			Id = Data.Id;
			HostName = Data.HostName;
			JobName = Data.JobName;
			Policy = Data.Policy;
			IsInit = Data.IsInit;

			CopyToList(Data);
		}
		public void CopyToList(JobData Data)
		{
			Path.Clear();
			foreach (var item in Data.Path) Path.Add(item);
			BlackPath.Clear();
			foreach (var item in Data.BlackPath) BlackPath.Add(item);
			BlackFile.Clear();
			foreach (var item in Data.BlackFile) BlackFile.Add(item);
			BlackFileExt.Clear();
			foreach (var item in Data.BlackFileExt) BlackFileExt.Add(item);
			WhiteFile.Clear();
			foreach (var item in Data.WhiteFile) WhiteFile.Add(item);
			WhiteFileExt.Clear();
			foreach (var item in Data.WhiteFileExt) WhiteFileExt.Add(item);

		}

		public override int GetHashCode()
		{
			HashCode hash = new HashCode();
			hash.Add(HostName);
			hash.Add(IsGlobalUser);
			hash.Add(UserID);
			hash.Add(StrPath);
			hash.Add(StrBlackPath);
			hash.Add(StrBlackFile);
			hash.Add(StrBlackFileExt);
			hash.Add(StrWhiteFile);
			hash.Add(StrWhiteFileExt);
			hash.Add(Policy);
			hash.Add(Remove);
			hash.Add(ScheduleList);
			return hash.ToHashCode();
		}

		public bool Equals(JobData x, JobData y)
		{
			if (x == null || y == null) return false;
			return x.Equals(y);
		}

		public int GetHashCode([DisallowNull] JobData obj)
		{
			return obj.GetHashCode();
		}
	}


	public class Schedule
	{
		/**************************Times***********************************/
		public static readonly int MaxHours = 24;
		public static readonly int MaxMins = 60;
		public static readonly int ONE_DAY = MaxHours * MaxMins;
		/**********************Day of the Week*****************************/
		public static readonly int EVERY = 0b_1000_0000;
		public static readonly int SUNDAY = 0b_0100_0000;
		public static readonly int MONDAY = 0b_0010_0000;
		public static readonly int TUESDAY = 0b_0001_0000;
		public static readonly int WEDNESDAY = 0b_0000_1000;
		public static readonly int THURSDAY = 0b_0000_0100;
		public static readonly int FRIDAY = 0b_0000_0010;
		public static readonly int SATURDAY = 0b_0000_0001;

		private static readonly int[] DayOfTheWeekValueList = [EVERY, SUNDAY, MONDAY, TUESDAY, WEDNESDAY, THURSDAY, FRIDAY, SATURDAY];
		private static readonly string[] DayOfTheWeekNameList = ["Every", "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
		/***********************Backup Type********************************/

		public int ID { get; set; }
		public int JobID { get; set; }
		public int Weeks { get; set; }
		public int AtTime { get; set; }
		public int ForHours { get; set; }
		public string StrWeek
		{
			get
			{
				var data = new StringBuilder();

				for (int i = 0; i < DayOfTheWeekValueList.Length; i++)
				{
					if ((Weeks & DayOfTheWeekValueList[i]) > 0)
						data.Append(DayOfTheWeekNameList[i]).Append(" ");
				}

				return data.ToString();
			}
		}
		public string StrAtTime { get => $"{AtTime / MaxMins:D02}:{AtTime % MaxMins:D02}"; }

		public Schedule()
		{
			ID = 0;
			JobID = 0;
			Weeks = 0;
			AtTime = 0;
			ForHours = 0;
		}

		public bool AddWeek(int Week)
		{
			if ((Weeks & Week) == 0) Weeks += Week;
			else return false;
			return true;
		}
		public bool DelWeek(int Week)
		{
			if ((Weeks & Week) > 0) Weeks -= Week;
			else return false;
			return true;
		}
		public void SetAtTime(int Hours, int Mins)
		{
			AtTime = Hours * 60 + Mins;
		}

		private bool CheckDayOfTheWeek(int DayOfTheWeekIndex)
		{
			if ((Weeks & EVERY) == EVERY) return true;

			if ((Weeks & DayOfTheWeekValueList[DayOfTheWeekIndex]) == DayOfTheWeekValueList[DayOfTheWeekIndex]) return true;
			return false;
		}
		private static int GetTodayIndex(string DayOfTheWeekName)
		{
			for (int i = 1; i < DayOfTheWeekNameList.Length; i++)
				if (DayOfTheWeekName.StartsWith(DayOfTheWeekNameList[i], System.StringComparison.OrdinalIgnoreCase))
					return i;
			return -1;
		}
		private static int GetYesterdayIndex(int DayOfTheWeekIndex)
		{
			int Yesterday = DayOfTheWeekIndex - 1;
			if (Yesterday < 1) Yesterday = DayOfTheWeekNameList.Length - 1;

			return Yesterday;
		}
		public bool IsExistSchedule(string DayOfTheWeekName, int Hours, int Mins)
		{
			int Now = Hours * 60 + Mins;             // 현재 시간
			int EndTimes = AtTime + (ForHours * 60); // 작업 종료시간
			int NextDayTimes = 0;                    // 추가 작업시간

			if (ForHours == 0) EndTimes = ONE_DAY;//0이라면 그날까지

			int DayOfTheWeekIndex = GetTodayIndex(DayOfTheWeekName);     //오늘 요일
			int YesterdayOfTheWeekIndex = GetYesterdayIndex(DayOfTheWeekIndex);//어제 요일

			if (DayOfTheWeekIndex < 0) return false;

			if (EndTimes > ONE_DAY) NextDayTimes = EndTimes - ONE_DAY;//24시간을 넘었을 경우 추가작업시간에 할당

			//case 1 : 정해진 요일, 작업시간인지 확인
			if (Now >= AtTime) return CheckDayOfTheWeek(DayOfTheWeekIndex);
			//case 2 : 24시간을 초과하여 추가 작업시간인지 확인
			else if (Now <= NextDayTimes) return CheckDayOfTheWeek(YesterdayOfTheWeekIndex);
			return false;
		}

		public bool Equals(Schedule Data)
		{
			if (Weeks != Data.Weeks) return false;
			if (AtTime != Data.AtTime) return false;
			if (ForHours != Data.ForHours) return false;
			return true;
		}
	}
}
