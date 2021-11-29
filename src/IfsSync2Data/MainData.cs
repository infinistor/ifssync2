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
        public const string COMPANY_NAME = "PSPACE";
        /************************** Main Flag *********************************/
        public const string ALIVE_CHECK = "Alive";
        public const int MY_TRUE = 1;
        public const int MY_FALSE = 0;
        public const int DEFAULT_STATUS_CHECK_DELAY = 1 * 1000; //1sec
        public const int DEFAULT_BINARY_SIZE = 1024;
        private const char PATHSEPARATOR = '\\';
        private const string PATHSEPARATORS = "\\";
        public const string AWS_FLAG = "-";
        /************************** Main Pass *********************************/
        public const string ROOT = "C:\\PSPACE\\";
        public const string DB_DIRECTORY_NAME = "DB\\";
        public const string DB_EXTENSION_NAME =".db";
        public const string MUTEX_GLOBAL_NAME = "Global\\";
        public const string EXE = ".exe";
        public const string REGISTRY_ROOT = "Software\\PSPACE\\IfsSync2\\";
        public const string NETDIRVER_REGISTRY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System";
        public const string NETDRIVER_ENABLELINKEDCONNECTIONS = "EnableLinkedConnections";

        public const string HTTP = "http://";
        public const string HTTPS = "https://";
        public const string UNKNOWN = "Unknown";
        /*************************** Main UI **********************************/
        public const string UI_NAME = "IfsSync2UI";
        public const string MAIN_STORAGE_NAME = "Default";
        public const string MUTEX_NAME_UI = MUTEX_GLOBAL_NAME + "{{" + UI_NAME + "}}";
        public const string UNC_TYPE_LINK_PATH = "\\target.lnk";

        /************************** Tray Icon *********************************/
        public const string TRAYICON_NAME = "IfsSync2TrayIcon";
        public const string MUTEX_NAME_TRAYICON = MUTEX_GLOBAL_NAME + "{" + TRAYICON_NAME + "}";
        public const string ICON_FILE_NAME = "file";
        public const string TRAYICON_CONFIG_PATH = REGISTRY_ROOT + "TrayIconConfig";
        
        /************************ Filter *******************************/
        public const string FILTER_NAME = "IfsSync2Filter";
        public const string FILTER_EXE = FILTER_NAME + EXE;
        public const string MUTEX_NAME_FILTER = MUTEX_GLOBAL_NAME + "{" + FILTER_NAME + "}";
        public const string FILTER_CONFIG_PATH = REGISTRY_ROOT + "FilterConfig";
        public const string FILTER_DRIVE_PATH = "Lib\\cbfilter.cab";
        public const int ALTITUDE_FAKE_VALUE_FOR_DEBUG = 360000;

        public const string RUNTIME_LICENSE_KEY = "----";
        /************************ Sender *******************************/
        public const string SENDER_NAME = "IfsSync2Sender";
        public const string SENDER_EXE = SENDER_NAME + EXE;
        public const string MUTEX_NAME_SENDER = MUTEX_GLOBAL_NAME + "{" + SENDER_NAME + "}";
        public const string SENDER_CONFIG_PATH = REGISTRY_ROOT + "SenderConfig";
        public const string mGuid = "{adf69b11-073c-493b-8dfe-888054f2fda3}";
        public const int SENDER_TIMEOUT = 3600; // sec
        public const int UPLOAD_CHANGE_FILE_SIZE = 1073741824; // 1GB
        public const int UPLOAD_PART_SIZE = 100 * 1024 * 1024; // 100mb

        /********************** Watcher Service *************************/
        public const string WATCHER_SERVICE_NAME = "IfsSync2WatcherService";
        public const string WATCHER_SERVICE_EXE = WATCHER_SERVICE_NAME + EXE;
        public const string WATCHER_CONFIG_PATH = REGISTRY_ROOT + "WatcherConfig";
        public const string WATCHER_SERVICE_GET_USER = "";
        public const string WATCHER_SERVICE_GET_JOBS = "";
        public const string WATCHER_SERVICE_PUT_ALIVE = "CheckAlive/";
        public const string WATCHER_SERVICE_VERSION_CHECK = "CheckUpdate";

        /********************** Instant Backup *********************************/
        public const string INSTANT_BACKUP_NAME = "Instant";
        public const string INSTANT_REGISTRY_ROOT_NAME = REGISTRY_ROOT + "Instant";

        /************************** Job Data *********************************/
        public const string MUTEX_NAME_JOB_SQL = MUTEX_GLOBAL_NAME + "{JobDataDB}";
        public const string DEFAULT_HOSTNAME_NAME = "Global";
        public const string DEFAULT_JOB_NAME = "Default";
        public const string JOB_CONFIG_NAME = "Job\\";
        public const string DEFAULT_BLACK_PATH_LIST = @"C:\$WINDOWS.~BT|___allroot___$Recycle.Bin|___allroot___JCK|C:\Program Files (x86)|C:\Program Files|C:\Windows|C:\Users\___alldir___\AppData|C:\ProgramData|C:\Documents and Settings|C:\Users\___alldir___\OneDriveTemp|___allroot___BACKUP|C:\System Volume Information|C:\Users\___alldir___\Dropbox|C:\Users\pspace\eclipse-workspace\IfsSync|";
        public const string DEFAULT_GLOBAL_JOB_NAME = "Global Backup ";
        public const string JOB_DB_FILE_NAME = "Job";

        /*********************** Extension Data *******************************/
        public const string EXTENSION_NAME = "Extension";
        public const string MUTEX_NAME_EXTENSION_NAME = MUTEX_GLOBAL_NAME + "{ExtensionDB}";
        public const string DEFAULT_EXTENSION_LIST = "mp3,wav,docx,doc,xlsx,xls,pdf,ppt,pptx,odt,ods,odp,rtf,txt,jpg,png,gif,tiff,ico,svg,webp,csv,json,xml,html,zip,pst,avi,mov,mp4,ogg,wmv,webm";

        /************************** User Data *********************************/
        public const string MUTEX_NAME_USER_SQL = MUTEX_GLOBAL_NAME + "{UserDataDB}";
        public const string USER_DB_FILE_NAME = "UserData";

        /************************** Curl Data *********************************/
        public const string CURL_GET_S3_VOLUME_SIZE = "/ifss30";
        public const string CURL_GET_S3_VOLUME_TOTAL_SIZE = "Total";
        public const string CURL_GET_S3_VOLUME_USED_SIZE = "Used";
        public const string CURL_STR_CONTENTTYPE = "application/json";
        public const string CURL_STR_POST_METHOD = "POST";
        public const string CURL_STR_GET_METHOD = "GET";
        public const string CURL_STR_PUT_METHOD = "PUT";
        public const int    CURL_TIMEOUT_DELAY = 20 * 1000; // 20 sec
        /************************** Curl Data *********************************/
        public const int    S3FILEMANAGER_DEFAULT_PORT = 5544;
        /***************************** ETC ************************************/
        public static readonly string[] CapacityUnitList = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string CreateFilePath(string TargetPath, string FileName)
        {
            if (!TargetPath.EndsWith(PATHSEPARATORS)) TargetPath += PATHSEPARATORS;
            return string.Format("{0}{1}.exe", TargetPath, FileName);
        }
        public static string CreateIconPath(string TargetPath, string IconName)
        {
            if (!TargetPath.EndsWith(PATHSEPARATORS)) TargetPath += PATHSEPARATORS;
            return string.Format("{0}{1}.ico", TargetPath, IconName);
        }
        public static string CreateDBFolderName(string TargetPath)
        {
            if (!TargetPath.EndsWith(PATHSEPARATORS)) TargetPath += PATHSEPARATORS;
            return string.Format("{0}{1}", TargetPath, DB_DIRECTORY_NAME);
        }
        public static string CreateDBFileName(string TargetPath, string FileName)
        {
            if (!TargetPath.EndsWith(PATHSEPARATORS)) TargetPath += PATHSEPARATORS;
            return string.Format("{0}{1}{2}{3}", TargetPath, DB_DIRECTORY_NAME, FileName, DB_EXTENSION_NAME);
        }
        public static string CreateDBFileNameAndHostName(string TargetPath, string HostName, string FileName)
        {
            if (!TargetPath.EndsWith(PATHSEPARATORS)) TargetPath += PATHSEPARATORS;
            return string.Format("{0}{1}{2}\\{3}{4}", TargetPath, DB_DIRECTORY_NAME, HostName, FileName, DB_EXTENSION_NAME);
        }
        public static bool CreateDirectory(string FilePath)
        {
            try
            {
                string MyPath = new FileInfo(FilePath).DirectoryName;
                Directory.CreateDirectory(MyPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static string CreateMutexName(string Name)
        {
            return string.Format("{0}{{{1}}}", MUTEX_GLOBAL_NAME, Name);
        }
        public static string CreateRegistryJobName(string HostName, string JobName)
        {
            return string.Format("{0}{1}{2}\\{3}", REGISTRY_ROOT, JOB_CONFIG_NAME, HostName, JobName);
        }
        public static string CreateAddress(string Address, string Port)
        {
            if (string.IsNullOrWhiteSpace(Address)) return "";
            if (string.IsNullOrWhiteSpace(Port)) return "";

            return string.Format("https://{0}:{1}/api/v1/IfsSyncClients/", Address, Port);
        }
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

            return string.Format("{0:0.0}{1}", Size, CapacityUnitList[UnitCount]);
        }

        public static string GetVersion()
        {
            Assembly assemObj = Assembly.GetExecutingAssembly();
            Version v = assemObj.GetName().Version; // 현재 실행되는 어셈블리..dll의 버전 가져오기

            int majorV = v.Major; // 주버전
            int minorV = v.Minor; // 부버전
            int buildV = v.Build; // 빌드번호
            int revisionV = v.Revision; // 수정번호

            return string.Format("{0}.{1}.{2}.{3}", majorV, minorV, buildV, revisionV);
        }

        public static string GetFileName(string FilePath)
        {
            string[] result = FilePath.Split(PATHSEPARATOR);
            string FileName = result[result.Length - 1];

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
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(FileName))
                    {
                        var hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        public static string CreateS3FileManagerURL(string Address, int Port = -1)
        {
            //if (!Address.StartsWith(HTTP, StringComparison.OrdinalIgnoreCase)) Address = HTTP + Address;
            //else if (Address.StartsWith(HTTPS, StringComparison.OrdinalIgnoreCase)) Address = Address.Replace(HTTPS, HTTP);
            if (Port <= 0) Port = S3FILEMANAGER_DEFAULT_PORT;
            return string.Format("{0}:{1}", Address, Port);
        }
        
        public static string GetLogFolder(string ProcessName)
        {
            return string.Format("{0}Log//{1}", ROOT, ProcessName);
        }
    }
}
