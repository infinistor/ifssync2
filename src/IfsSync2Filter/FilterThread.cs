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
using IfsSync2Data;
using callback.CBFSFilter;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;

namespace IfsSync2Filter
{
	public class FilterThread
	{
		static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		/***************************** Job Data *************************************/
		public readonly JobData Job;
		readonly JobStatus State;
		/*************************** Filter Data ************************************/
		readonly Cbmonitor monitor = null;
		const long DEFAULT_NOTIFY_FILTER = Constants.FS_NE_CREATE |
											Constants.FS_NE_DELETE |
											Constants.FS_NE_RENAME |
											Constants.FS_NE_WRITE |
											Constants.FS_NE_SET_SIZES |
											Constants.FS_NE_CAN_DELETE;

		const string RESERVED_WORDS_ALLROOT = "___allroot___";
		const string RESERVED_WORDS_ALLDIR = "___alldir___";
		const string RESERVED_WORDS_DIR = "*";
		const string DEFAULT_DATE_FORMAT = "yyyy-MM-dd-HH:mm:ss";
		readonly string Net = @"\Device\LanmanRedirector\;";
		public const string MS_MP_ENG = "MsMpEng.exe";

		readonly TaskDbManager _taskDb;

		readonly List<string> DeleteStacks = [];

		readonly WriteFileManager _writeFileManager;

		public bool IsAlive { get; set; }
		public bool IsFilterUpdate { get; set; }

		public void FilterStateOn()
		{
			State.Filter = true;
		}
		public FilterThread(JobData job)
		{
			IsAlive = true;
			IsFilterUpdate = false;
			Job = job;
			_taskDb = new(job.JobName);
			State = new(Job.HostName, Job.JobName, true);
			monitor = new(MainData.RUNTIME_LICENSE_KEY);

			// WriteFileManager 초기화
			_writeFileManager = new WriteFileManager(CreateBackup);

			monitor.OnNotifyDeleteFile += NotifyDeleteFile;
			log.Debug("NotifyDeleteFile handler added");
			monitor.OnNotifyRenameOrMoveFile += NotifyRenameOrMoveFile;
			log.Debug("NotifyRenameOrMoveFile handler added");
			monitor.OnNotifyWriteFile += NotifyWriteFile;
			log.Debug("NotifyWriteFile handler added");
			monitor.OnNotifyCreateFile += NotifyCreateFile;
			log.Debug("NotifyCreateFile handler added");
			monitor.OnNotifySetFileSize += NotifySetFileSize;
			log.Debug("NotifySetFileSize handler added");
			monitor.OnNotifyCanFileBeDeleted += NotifyCanFileBeDeleted;
			log.Debug("NotifyCanFileBeDeleted handler added");
			monitor.OnNotifyGetFileSizes += NotifyGetFileSizes;
			log.Debug("NotifyGetFileSizes handler added");

			//Filter Update
			FilterUpdate();

			//Start Filter
			if (!monitor.Active)
			{
				monitor.ProcessFailedRequests = false;
				monitor.Initialize(MainData.mGuid);
				monitor.StartFilter();
				State.Filter = true;
				log.Info("Filter started");
			}
		}

		public List<string> GetFilterList()
		{
			var filterList = new List<string>();

			foreach (var path in Job.Path)
			{
				string directory = path.Trim();
				if (!directory.EndsWith('\\')) directory += "\\";

				filterList.Add(directory + "*");
				filterList.Add(directory);
			}
			return filterList;
		}

		public void FilterUpdate()
		{
			List<string> filters = GetFilterList();

			bool failCheck = false;

			monitor.DeleteAllFilterRules();
			foreach (string filter in filters)
			{
				try
				{
					//NUC Folder Check
					if (MainData.CheckUNCFolder(filter))
					{
						monitor.AddFilterRule(filter, DEFAULT_NOTIFY_FILTER);
						log.Debug($"NUC Folder Filter : {filter}");
					}
					//Drive Check
					else if (!MainData.IsDriveAccessible(filter, out string error))
					{
						failCheck = true;
						log.Debug($"Drive Check Fail : {filter}, Error: {error}");
					}
					else
					{
						monitor.AddFilterRule(filter, DEFAULT_NOTIFY_FILTER);
						log.Debug($"Filter : {filter}");
					}
				}
				catch (Exception e)
				{
					failCheck = true;
					log.Error(e);
				}
			}
			SetBlackPath();
			log.Info("Filter Update");

			if (failCheck) IsFilterUpdate = false;
			else IsFilterUpdate = true;
		}

		List<string> GetAllRoot()
		{
			var roots = new List<string>();

			foreach (string MyPath in Job.Path)
			{
				string Root = Path.GetPathRoot(MyPath);
				if (!Root.EndsWith('\\')) Root += "\\";
				if (!roots.Contains(Root)) roots.Add(Root);
			}
			return roots;
		}

		List<string> GetBlackPathList()
		{
			var BlackPathList = new List<string>();

			var AllRoot = GetAllRoot();

			foreach (string MyPath in Job.BlackPath)
			{
				if (string.IsNullOrWhiteSpace(MyPath)) continue;
				string BlackPath = ReplacementOfReservedWords(MyPath).Trim();

				if (BlackPath.StartsWith(RESERVED_WORDS_ALLROOT))
				{
					foreach (string Root in AllRoot)
					{
						string Item = BlackPath.Replace(RESERVED_WORDS_ALLROOT, Root);
						BlackPathList.Add(Item);
					}
				}
				else BlackPathList.Add(BlackPath);
			}

			return BlackPathList;
		}


		void SetBlackPath()
		{
			monitor.DeleteAllPassthroughRules();
			var BlackPathList = GetBlackPathList();

			foreach (string BlackPath in BlackPathList) monitor.AddPassthroughRule(BlackPath, DEFAULT_NOTIFY_FILTER);
		}
		public void JobDataUpdate(JobData Data)
		{
			Job.CopyTo(Data);
			FilterUpdate();
		}
		public void Close()
		{
			if (monitor != null)
			{
				monitor.StopFilter(false);
				monitor.DeleteAllFilterRules();
			}
			State.Filter = false;
			_writeFileManager.Stop();
			_taskDb.DeleteDBFile();
		}

		#region Sql Task Upload

		static bool CheckWhiteFileList(string FileName, ObservableCollection<string> WhiteFileExt)
		{
			// 파일 이름이 비어있거나 null인 경우 처리
			if (string.IsNullOrWhiteSpace(FileName))
				return false;

			// FilePathException 체크
			if (FilePathException(FileName))
				return false;

			// WhiteFileExt에 "ALL"이 포함되어 있거나, 파일 확장자가 WhiteFileExt 목록에 있는 경우 허용
			return WhiteFileExt.Any(ext =>
				ext.Equals("ALL", StringComparison.OrdinalIgnoreCase) ||
				FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
		}


		static List<string> SubDirectory(string ParentDirectory, ObservableCollection<string> ExtList)
		{
			DirectoryInfo dInfoParent = new(ParentDirectory);
			List<string> FileList = [];
			if (!dInfoParent.Exists) return FileList;
			log.Debug($"Processing directory {ParentDirectory}, found {dInfoParent.GetDirectories().Length} subdirectories");

			//add Folder List
			foreach (DirectoryInfo dInfo in dInfoParent.GetDirectories())
			{
				try { FileList.AddRange(SubDirectory(dInfo.FullName, ExtList)); }
				catch
				{
					log.Error($"Error processing directory: {dInfo.FullName}");
				}
			}
			FileList.AddRange(AddBackupFile(ParentDirectory, ExtList));

			return FileList;
		}

		static List<string> AddBackupFile(string ParentDirectory, ObservableCollection<string> ExtList)
		{
			//add File List
			string[] files = Directory.GetFiles(ParentDirectory);
			log.Debug($"Found {files.Length} files in {ParentDirectory}");
			List<string> FileList = [];

			// 파일 나열
			foreach (string file in files)
			{
				FileInfo info = new(file);
				if (!info.Exists) continue;
				if ((info.Attributes & FileAttributes.System) == FileAttributes.System) { /*empty*/ }
				else if (CheckWhiteFileList(info.Name, ExtList))
				{
					FileList.Add(info.FullName);
				}
			}
			log.Debug($"Added {FileList.Count} files from {ParentDirectory}");
			return FileList;
		}

		static List<string> GetFileListToDirectory(string Directory, ObservableCollection<string> ExtList)
		{
			return SubDirectory(Directory, ExtList);
		}

		static long GetFileSize(string FilePath)
		{
			try { return new FileInfo(FilePath).Length; }
			catch
			{
				log.Error($"Error getting size for file: {FilePath}");
				return 0;
			}
		}

		void CreateBackup(string FilePath)
		{
			string FileName = MainData.GetFileName(FilePath);
			log.Debug($"Processing: {FilePath}");

			// 폴더인 경우
			if (CheckIsFolder(FilePath))
			{
				log.Debug($"Folder detected: {FilePath}");
				List<string> FileList = GetFileListToDirectory(FilePath, Job.WhiteFileExt);
				log.Debug($"Found {FileList.Count} files in folder");
				foreach (string File in FileList)
				{
					long fileSize = GetFileSize(File);
					TaskData Data = new(TaskData.TaskTypeList.Upload, File, DateTime.Now.ToString(DEFAULT_DATE_FORMAT), fileSize);
					_taskDb.Insert(Data);
					log.Debug($"SubFile Upload: {Data.FilePath}");
				}
			}
			// 파일인 경우
			else if (CheckWhiteFileList(FileName, Job.WhiteFileExt))
			{
				long fileSize = GetFileSize(FilePath);
				TaskData Data = new(TaskData.TaskTypeList.Upload, FilePath, DateTime.Now.ToString(DEFAULT_DATE_FORMAT), fileSize);
				_taskDb.Insert(Data);
				log.Debug($"File Upload: {Data.FilePath}");
			}
		}

		void DeleteBackup(string FilePath)
		{
			string FileName = MainData.GetFileName(FilePath);

			if (CheckWhiteFileList(FileName, Job.WhiteFileExt))
			{
				TaskData Data = new(TaskData.TaskTypeList.Delete, FilePath, DateTime.Now.ToString(DEFAULT_DATE_FORMAT));
				_taskDb.Insert(Data);
				log.Debug(Data.FilePath);
			}
		}

		void RenameBackup(string FilePath, string NewFilePath)
		{
			string NewFileName = MainData.GetFileName(NewFilePath);

			// 폴더인 경우
			if (CheckIsFolder(NewFilePath))
			{
				log.Debug($"Folder detected: {FilePath} => {NewFilePath}");

				// 폴더인 경우 폴더 하위 모든 파일을 읽어서 rename 처리
				List<string> FileList = GetFileListToDirectory(NewFilePath, Job.WhiteFileExt);
				log.Debug($"Number of files in folder: {FileList.Count}");
				foreach (string File in FileList)
				{
					var oldFilePath = File.Replace(NewFilePath, FilePath);
					TaskData Data = new(TaskData.TaskTypeList.Rename, oldFilePath, DateTime.Now.ToString(DEFAULT_DATE_FORMAT), File);
					_taskDb.Insert(Data);
					log.Debug($"SubFile Rename : {Data.FilePath} => {Data.NewFilePath}");
				}
			}
			// 파일인 경우
			else if (CheckWhiteFileList(NewFileName, Job.WhiteFileExt))
			{
				TaskData Data = new(TaskData.TaskTypeList.Rename, FilePath, DateTime.Now.ToString(DEFAULT_DATE_FORMAT), NewFilePath);
				_taskDb.Insert(Data);
				log.Debug($"File Rename : {Data.FilePath} => {Data.NewFilePath}");
			}
		}
		#endregion Sql Task Upload

		#region ETC
		string ChangeHardLinkDriveName(string FilePath)
		{
			//\Device\LanmanRedirector\;<drive letter>:<logon-session id>\<server>\<share>\<path>"
			//Get <drive letter>:<path>
			if (FilePath.StartsWith(Net))
			{
				string from = FilePath.Replace(Net, ""); //delete [\Device\LanmanRedirector\;]
				string VolumeName = from[..2]; //Get <drive letter>:

				string[] result = from.Split('\\'); //cut <drive letter>:<logon-session id>, <server>, <share>, <path> ....

				StringBuilder newFilePath = new(VolumeName);
				for (int i = 3; i < result.Length; i++)
				{
					newFilePath.Append('\\').Append(result[i]);
				}
				return newFilePath.ToString();
			}
			else return FilePath;
		}
		#endregion ETC
		#region Filter Notify Event

		void NotifyRenameOrMoveFile(object Sender, CbmonitorNotifyRenameOrMoveFileEventArgs args)
		{
			log.Debug($"{args.FileName} => {args.NewFileName}");

			string FilePath = ChangeHardLinkDriveName(args.FileName);
			string NewFilePath = ChangeHardLinkDriveName(args.NewFileName);

			FilterEventHandler.EventList Event = FilterEventHandler.FindSaveByRenameEvent(Job.Path, FilePath, NewFilePath);

			string Msg;
			switch (Event)
			{
				case FilterEventHandler.EventList.Rename: RenameBackup(FilePath, NewFilePath); Msg = $"Rename      : {FilePath} => {NewFilePath}"; break;
				case FilterEventHandler.EventList.SaveFile: CreateBackup(FilePath); Msg = $"SaveFile    : {FilePath}"; break;
				case FilterEventHandler.EventList.SaveNewFile: CreateBackup(NewFilePath); Msg = $"SaveNewFile : {FilePath}"; break;
				case FilterEventHandler.EventList.Delete: DeleteBackup(FilePath); Msg = $"Delete      : {FilePath}"; break;
				case FilterEventHandler.EventList.None:
				default: return;
			}

			log.Debug(Msg);
		}
		void NotifyCreateFile(object Sender, CbmonitorNotifyCreateFileEventArgs args)
		{
			log.Debug($"{args.FileName}");

			// 생성 이벤트가 폴더가 아닌 경우 백업
			if (!CheckIsFolder(args.FileName)) CreateBackup(args.FileName);
		}
		#region File Write Event

		void NotifySetFileSize(object Sender, CbmonitorNotifySetFileSizeEventArgs args)
		{
			log.Debug($"Processing: {args.FileName}");
			_writeFileManager.AddOrUpdateFile(args.FileName);
		}
		void NotifyGetFileSizes(object Sender, CbmonitorNotifyGetFileSizesEventArgs args)
		{
			log.Debug($"Processing: {args.FileName}");

			// 특정 프로세스에서 수정중인 파일 추가.
			string ProcessName = ((Cbmonitor)Sender).GetOriginatorProcessName();
			if (ProcessName.EndsWith(MS_MP_ENG, StringComparison.OrdinalIgnoreCase))
				_writeFileManager.AddOrUpdateFile(args.FileName);
		}
		void NotifyWriteFile(object Sender, CbmonitorNotifyWriteFileEventArgs args)
		{
			log.Debug($"Processing: {args.FileName}");
			_writeFileManager.AddOrUpdateFile(args.FileName);
		}
		#endregion File Write Event

		#region File Delete Event
		void NotifyDeleteFile(object Sender, CbmonitorNotifyDeleteFileEventArgs args)
		{
			log.Debug($"Processing: {args.FileName}");
			DeleteBackup(args.FileName);
		}

		void NotifyCanFileBeDeleted(object Sender, CbmonitorNotifyCanFileBeDeletedEventArgs args)
		{
			log.Debug($"Processing: {args.FileName}");

			if (!args.CanDelete)
			{
				int index = DeleteStacks.IndexOf(args.FileName);
				if (index < 0)
				{
					DeleteStacks.Add(args.FileName);
					log.Debug($"Added to delete stack: {args.FileName}");
				}
				else
				{
					log.Debug($"Deleting: {args.FileName}");
					DeleteStacks.RemoveAt(index);
					DeleteBackup(args.FileName);
				}
			}
		}
		#endregion File Delete Event
		#endregion Filter Notify Event
		#region Util

		static string ReplacementOfReservedWords(string Path)
		{
			return Path.Replace(RESERVED_WORDS_ALLDIR, RESERVED_WORDS_DIR) + "\\*";
		}
		static bool CheckIsFolder(string FilePath)
		{
			try
			{
				var info = new DirectoryInfo(FilePath);
				if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) return true;
				return false;
			}
			catch
			{
				return false;
			}
		}
		static bool FilePathException(string FilePath)
		{
			string FileName = MainData.GetFileName(FilePath);

			if (FileName.StartsWith('~')) return true;
			else if (FileName.EndsWith("tmp", StringComparison.OrdinalIgnoreCase)) return true;
			return false;
		}
		#endregion
	}
}
