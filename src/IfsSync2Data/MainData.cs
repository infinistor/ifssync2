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
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace IfsSync2Data
{
	public static class MainData
	{
		#region Global Variable
		public const string COMPANY_NAME = "PSPACE";
		public const string ALIVE_CHECK = "Alive";
		public const int MY_TRUE = 1;
		public const int MY_FALSE = 0;
		public const int DEFAULT_STATUS_CHECK_DELAY = 1 * 1000; //1sec
		public const int DEFAULT_BINARY_SIZE = 1024;
		public const string AWS_FLAG = "-";
		public const string UNKNOWN = "Unknown";
		#endregion
		#region Global Path
		public const string ROOT = "C:\\PSPACE\\";
		public const string DB_DIRECTORY_NAME = ROOT + "DB\\";
		public const string LOG_DIRECTORY_NAME = ROOT + "LOG\\";
		public const string DB_EXTENSION_NAME = "db";
		public const string MUTEX_GLOBAL_NAME = "Global\\";
		public const string EXE = ".exe";
		public const string REGISTRY_ROOT = "Software\\PSPACE\\IfsSync2\\";
		public const string NET_DRIVER_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
		public const string NET_DRIVER_ENABLE_LINKED_CONNECTIONS = "EnableLinkedConnections";

		public const string HTTP = "http://";
		public const string HTTPS = "https://";
		#endregion
		#region UI
		public const string UI_NAME = "IfsSync2UI";
		public const string MAIN_STORAGE_NAME = "Default";
		public const string MUTEX_NAME_UI = MUTEX_GLOBAL_NAME + "{{" + UI_NAME + "}}";
		public const string UNC_TYPE_LINK_PATH = "\\target.lnk";
		#endregion
		#region Tray Icon
		public const string TRAY_ICON_NAME = "IfsSync2TrayIcon";
		public const string MUTEX_NAME_TRAY_ICON = MUTEX_GLOBAL_NAME + "{" + TRAY_ICON_NAME + "}";
		public const string ICON_FILE_NAME = "file";
		public const string TRAY_ICON_CONFIG_PATH = REGISTRY_ROOT + "TrayIconConfig";
		#endregion
		#region Filter
		public const string FILTER_NAME = "IfsSync2Filter";
		public const string FILTER_EXE = FILTER_NAME + EXE;
		public const string MUTEX_NAME_FILTER = MUTEX_GLOBAL_NAME + "{" + FILTER_NAME + "}";
		public const string FILTER_CONFIG_PATH = REGISTRY_ROOT + "FilterConfig";
		public const string FILTER_DRIVE_PATH = "Lib\\cbfilter.cab";
		public const int ALTITUDE_FAKE_VALUE_FOR_DEBUG = 360000;

		public const string RUNTIME_LICENSE_KEY = "43464E4641444E585246323032313035323336314D3935353434000000000000000000000000000046534143594A4D550000424D54304E304D563539524D0000";
		#endregion
		#region Sender
		public const string SENDER_NAME = "IfsSync2Sender";
		public const string SENDER_EXE = SENDER_NAME + EXE;
		public const string MUTEX_NAME_SENDER = MUTEX_GLOBAL_NAME + "{" + SENDER_NAME + "}";
		public const string SENDER_CONFIG_PATH = REGISTRY_ROOT + "SenderConfig";
		public const string mGuid = "{adf69b11-073c-493b-8dfe-888054f2fda3}";
		public const int SENDER_TIMEOUT = 3600; // sec
		public const int UPLOAD_CHANGE_FILE_SIZE = 1073741824; // 1GB
		public const int UPLOAD_PART_SIZE = 100 * 1024 * 1024; // 100mb
		#endregion
		#region Watcher
		public const string WATCHER_SERVICE_NAME = "IfsSync2WatcherService";
		public const string WATCHER_SERVICE_EXE = WATCHER_SERVICE_NAME + EXE;
		public const string WATCHER_CONFIG_PATH = REGISTRY_ROOT + "WatcherConfig";
		public const string WATCHER_SERVICE_GET_USER = "";
		public const string WATCHER_SERVICE_GET_JOBS = "";
		public const string WATCHER_SERVICE_PUT_ALIVE = "CheckAlive/";
		public const string WATCHER_SERVICE_VERSION_CHECK = "CheckUpdate";
		#endregion
		#region Instant Backup
		public const string INSTANT_BACKUP_NAME = "Instant";
		public const string INSTANT_REGISTRY_ROOT_NAME = REGISTRY_ROOT + "Instant";
		#endregion
		#region Job Data
		public const string MUTEX_NAME_JOB_SQL = MUTEX_GLOBAL_NAME + "{JobDataDB}";
		public const string DEFAULT_HOSTNAME_NAME = "Global";
		public const string DEFAULT_JOB_NAME = "Default";
		public const string JOB_CONFIG_NAME = "Job\\";
		public const string DEFAULT_BLACK_PATH_LIST = @"C:\$WINDOWS.~BT|___allroot___$Recycle.Bin|___allroot___JCK|C:\Program Files (x86)|C:\Program Files|C:\Windows|C:\Users\___alldir___\AppData|C:\ProgramData|C:\Documents and Settings|C:\Users\___alldir___\OneDriveTemp|___allroot___BACKUP|C:\System Volume Information|C:\Users\___alldir___\Dropbox|C:\Users\pspace\eclipse-workspace\IfsSync|";
		public const string DEFAULT_GLOBAL_JOB_NAME = "Global Backup ";
		public const string JOB_DB_FILE_NAME = "Job";
		#endregion
		#region Extension Data
		public const string EXTENSION_NAME = "Extension";
		public const string MUTEX_NAME_EXTENSION_NAME = MUTEX_GLOBAL_NAME + "{ExtensionDB}";
		public const string DEFAULT_EXTENSION_LIST = "mp3,wav,docx,doc,xlsx,xls,pdf,ppt,pptx,odt,ods,odp,rtf,txt,jpg,png,gif,tiff,ico,svg,webp,csv,json,xml,html,zip,pst,avi,mov,mp4,ogg,wmv,webm";
		#endregion
		#region User Data
		public const string MUTEX_NAME_USER_SQL = MUTEX_GLOBAL_NAME + "{UserDataDB}";
		public const string USER_DB_FILE_NAME = "UserData";
		#endregion
		#region Curl Data
		public const string CURL_GET_S3_VOLUME_SIZE = "/ifss30";
		public const string CURL_GET_S3_VOLUME_TOTAL_SIZE = "Total";
		public const string CURL_GET_S3_VOLUME_USED_SIZE = "Used";
		public const string CURL_STR_CONTENT_TYPE = "application/json";
		public const string CURL_STR_POST_METHOD = "POST";
		public const string CURL_STR_GET_METHOD = "GET";
		public const string CURL_STR_PUT_METHOD = "PUT";
		public const int CURL_TIMEOUT_DELAY = 20 * 1000; // 20 sec
		public const int S3_FILE_MANAGER_DEFAULT_PORT = 5544;
		#endregion
		#region etc
		public static readonly string[] CapacityUnitList = new string[] { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		public const int DEFAULT_DELETE_DATE = 30;
		#endregion

		#region Utility
		public static string CreateExeFilePath(string TargetPath, string FileName) => Path.Combine(TargetPath, FileName + ".exe");
		public static string CreateIconFilePath(string TargetPath, string IconName) => Path.Combine(TargetPath, IconName + ".ico");
		public static bool CreateFile(string FilePath)
		{
			try
			{
				// 폴더가 없으면 생성
				if (!Directory.Exists(Path.GetDirectoryName(FilePath)))
					Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

				// 파일이 없으면 생성
				if (!File.Exists(FilePath))
					File.Create(FilePath).Close();

				return true;
			}
			catch
			{
				return false;
			}
		}

		public static string CreateMutexName(string Name) => $"{MUTEX_GLOBAL_NAME}{{{Name}}}";
		public static string CreateRegistryJobName(string HostName, string JobName) => $"{REGISTRY_ROOT}{JOB_CONFIG_NAME}{HostName}\\{JobName}";
		public static string CreateAddress(string Address, string Port) => (string.IsNullOrWhiteSpace(Address) || string.IsNullOrWhiteSpace(Port)) ? "" : $"https://{Address}:{Port}/api/v1/IfsSyncClients/";
		public static string SizeToString(long value)
		{
			const float IECPrefix = 1024.0F;
			const float MaxValue = 1000.0f;

			int UnitCount = 0;

			float Size = value;
			while (Size > MaxValue)
			{
				Size /= IECPrefix;
				UnitCount++;
			}

			return $"{Size:0.0}{CapacityUnitList[UnitCount]}";
		}

		public static string GetVersion()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			Version v = assembly.GetName().Version; // 현재 실행되는 어셈블리..dll의 버전 가져오기

			return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
		}

		public static string GetFileName(string FilePath)
		{
			string[] result = FilePath.Split(Path.DirectorySeparatorChar);
			string FileName = result[^1];

			return FileName;
		}

		public static bool CheckUNCFolder(string RootPath)
		{
			return RootPath.StartsWith(@"\\");
		}

		public static string CalculateMD5(string FileName)
		{
			try
			{
				using var md5 = MD5.Create();
				using var stream = File.OpenRead(FileName);
				var hash = md5.ComputeHash(stream);
				return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
			}
			catch
			{
				return string.Empty;
			}
		}

		public static string CreateS3FileManagerURL(string Address, int Port = -1)
		{
			if (Port <= 0) Port = S3_FILE_MANAGER_DEFAULT_PORT;
			return string.Format("{0}:{1}", Address, Port);
		}

		public static string GetLogFolder(string ProcessName) => $"{LOG_DIRECTORY_NAME}{ProcessName}";
		public static string GetDBFilePath(string dbName) => $"{DB_DIRECTORY_NAME}{dbName}.{DB_EXTENSION_NAME}";

		//파일삭제 함수
		public static void DeleteOldLogs(string dirPath, int DeleteDate = DEFAULT_DELETE_DATE)
		{
			var dirInfo = new DirectoryInfo(dirPath);
			if (!dirInfo.Exists) return;
			DateTime fileCreatedTime;
			DateTime cmpTime = DateTime.Now.AddDays(DeleteDate);

			foreach (FileInfo file in dirInfo.GetFiles())
			{
				fileCreatedTime = file.CreationTime;

				//파일생성날짜가 strDate보다 이전이면 파일을 삭제한다. 7일전이면 삭제
				if (DateTime.Compare(fileCreatedTime, cmpTime) > 0) File.Delete(file.FullName);
			}
		}
		#endregion
	}
}
