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
using IfsSync2Data;
using callback.CBFSFilter;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;

namespace IfsSync2Filter
{
    public class FilterThread
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        /***************************** Job Data *************************************/
        public readonly JobData Job;
        private readonly JobState State;
        /*************************** Filter Data ************************************/
        private readonly Cbmonitor monitor = null;
        private const long DEFULT_NOTIFY_FILTER =  //Constants.FS_NE_NONE;
                                                   //Constants.FS_NE_SET_SECURITY |
                                                   Constants.FS_NE_SET_SIZES |
                                                   Constants.FS_NE_CREATE |
                                                   Constants.FS_NE_DELETE |
                                                   Constants.FS_NE_RENAME |
                                                   Constants.FS_NE_WRITE |
                                                   Constants.FS_NE_SET_SIZES |
                                                   Constants.FS_NE_CAN_DELETE;

        private readonly static string RESERVEDWORDS_ALLROOT = "___allroot___";
        private readonly static string RESERVEDWORDS_ALLDIR = "___alldir___";
        private readonly static string RESERVEDWORDS_DIR = "*";
        private readonly string Net = @"\Device\LanmanRedirector\;";
        
        /***************************** Event Data ************************************/
        public const string MSMPENG = "MsMpEng.exe";

        private readonly List<string> DeleteStacks; //Delete Stack

        //Create Stack
        private readonly List<WriteFileInfo> NotifyWriteFileList;

        public class WriteFileInfo
        {
            public string FilePath = string.Empty;
            public long Size = 0;

            public WriteFileInfo(string _FilePath, long _Size)
            {
                FilePath = _FilePath;
                Size = _Size;
            }
            public void SizeUpdate(long _Size) { if (Size < _Size) Size = _Size; }
        }
        /****************************** SQL Data ************************************/
        private readonly TaskDataSqlManager TaskSQL;
        /******************************** ETC ***************************************/
        public bool IsAlive { get; set; }
        public bool IsFilterUpdate { get; set; }
        public void FilterStateOn()
        {
            State.Filter = true;
        }
        public FilterThread(string RootPath, JobData Data)
        {
            IsAlive = true;
            IsFilterUpdate = false;
            Job = Data;
            TaskSQL = new TaskDataSqlManager(RootPath, Job.HostName, Job.JobName);
            State = new JobState(Job.HostName, Job.JobName, true);
            NotifyWriteFileList = new List<WriteFileInfo>();
            DeleteStacks = new List<string>();
            monitor = new Cbmonitor(MainData.RUNTIME_LICENSE_KEY);
            
            monitor.OnNotifyDeleteFile       += NotifyDeleteFile;
            monitor.OnNotifyRenameOrMoveFile += NotifyRenameOrMoveFile;
            monitor.OnNotifyWriteFile        += NotifyWriteFile;
            monitor.OnNotifyCreateFile       += NotifyCreateFile;
            monitor.OnNotifySetFileSize      += NotifySetFileSize;
            monitor.OnNotifyCanFileBeDeleted += NotifyCanFileBeDeleted;
            monitor.OnNotifyGetFileSizes     += NotifyGetFileSizes;

            //Filter Update
            FilterUpdate();

            //Start Filter
            if (!monitor.Active)
            {
                monitor.ProcessFailedRequests = false;
                monitor.Initialize(MainData.mGuid);
                monitor.StartFilter();
                State.Filter = true;
                log.Info("Start");
            }
        }

        public List<string> GetFilterList()
        {
            List<string> FilterList = new List<string>();
            FilterList.Clear();

            foreach (var Dir in Job.Path)
            {
                string Directory = Dir.Trim();
                if (!Directory.EndsWith("\\")) Directory += "\\";

                FilterList.Add(string.Format("{0}*", Directory));
                FilterList.Add(string.Format("{0}", Directory));
            }
            return FilterList;
        }

        public void FilterUpdate()
        {
            List<string> FilterList = GetFilterList();

            bool FailCheck = false;

            monitor.DeleteAllFilterRules();
            foreach (string Filter in FilterList)
            {
                try
                {
                    //NUC Folder Check
                    if (MainData.CheckUNCFolder(Filter))
                    {
                        monitor.AddFilterRule(Filter, DEFULT_NOTIFY_FILTER);
                        log.Debug($"NUC Folder Filter : {Filter}");
                    }
                    //Drive Check
                    else if (!new DriveInfo(Path.GetPathRoot(Filter)).IsReady)
                    {
                        FailCheck = true;
                        log.Debug($"Filter Update Fail : {Filter}");
                    }
                    else
                    {
                        monitor.AddFilterRule(Filter, DEFULT_NOTIFY_FILTER);
                        log.Debug($"Filter : {Filter}");
                    }
                }
                catch(Exception e)
                {
                    FailCheck = true;
                    log.Error(e);
                }
            }
            SetBlackPath();
            log.Info("Filter Update");

            if (FailCheck) IsFilterUpdate = false;
            else           IsFilterUpdate = true;
        }

        private List<string> GetAllRoot()
        {
            List<string> Roots = new List<string>();

            foreach(string MyPath in Job.Path)
            {
                string Root = Path.GetPathRoot(MyPath);
                if (!Root.EndsWith("\\")) Root += "\\";
                if (!Roots.Contains(Root)) Roots.Add(Root);
            }
            return Roots;
        }

        private List<string> GetBlackPathList()
        {
            List<string> BlackPathList = new List<string>();

            List<string> AllRoot = GetAllRoot();

            foreach (string MyPath in Job.BlackPath)
            {
                if (string.IsNullOrWhiteSpace(MyPath)) continue;
                string BlackPath = ReplacementOfReservedWords(MyPath).Trim();

                if(BlackPath.StartsWith(RESERVEDWORDS_ALLROOT))
                {
                    foreach(string Root in AllRoot)
                    {
                        string Item = BlackPath.Replace(RESERVEDWORDS_ALLROOT, Root);
                        BlackPathList.Add(Item);
                    }
                }
                else BlackPathList.Add(BlackPath);
            }

            return BlackPathList;
        }

        private string ReplacementOfReservedWords(string Path)
        {
            return Path.Replace(RESERVEDWORDS_ALLDIR, RESERVEDWORDS_DIR) + "\\*";
        }

        private void SetBlackPath()
        {
            monitor.DeleteAllPassthroughRules();
            List<string> BlackPathList = GetBlackPathList();

            foreach (string BlackPath in BlackPathList) monitor.AddPassthroughRule(BlackPath, DEFULT_NOTIFY_FILTER);
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
            TaskSQL.DeleteDBFile();
        }

        #region Sql Task Upload

        private bool CheckIsFolder(string FilePath)
        {
            try
            {
                DirectoryInfo info = new DirectoryInfo(FilePath);
                if ((info.Attributes & FileAttributes.Directory) == FileAttributes.Directory) return true;
            }
            catch { }
            return false;
        }
        private bool CheckWhiteFileList(string FileName)
        {
            foreach (string Ext in Job.WhiteFileExt) if (FileName.EndsWith(Ext, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private bool FilePathException(string FilePath)
        {
            string FileName = MainData.GetFileName(FilePath);

            if (FileName.StartsWith("~")) return true;
            else if (FileName.EndsWith("tmp", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private List<string> SubDirectory(string ParentDirectory, ObservableCollection<string> ExtList)
        {
            DirectoryInfo dInfoParent = new DirectoryInfo(ParentDirectory);
            List<string> FileList = new List<string>();
            if (!dInfoParent.Exists) return FileList;

            //add Folder List
            foreach (DirectoryInfo dInfo in dInfoParent.GetDirectories())
            {
                try { FileList.AddRange(SubDirectory(dInfo.FullName, ExtList)); } catch { };
            }
            FileList.AddRange(AddBackupFile(ParentDirectory, ExtList));

            return FileList;
        }
        private List<string> AddBackupFile(string ParentDirectory, ObservableCollection<string> ExtList)
        {
            //add File List
            string[] files = Directory.GetFiles(ParentDirectory);
            List<string> FileList = new List<string>();

            foreach (string file in files)    // 파일 나열
            {
                FileInfo info = new FileInfo(file);
                if (!info.Exists) continue;
                if ((info.Attributes & FileAttributes.System) == FileAttributes.System) { /*empty*/ }
                else if (ExtList.Contains(info.Extension.Replace(".", "").ToLower()))
                {
                    if (!FilePathException(info.Name)) FileList.Add(info.FullName);
                }
            }
            return FileList;
        }
        private List<string> GetFileListToDirectory(string Directory, ObservableCollection<string> ExtList)
        {
            return SubDirectory(Directory, ExtList);
        }

        private long GetFileSize(string FilePath)
        {
            try { return new FileInfo(FilePath).Length; } catch { return 0; }
        }
        private bool CreateBackup(string FilePath)
        {
            string FileName = MainData.GetFileName(FilePath);

            if (!FilePathException(FileName))
            {
                if (CheckIsFolder(FilePath))
                {
                    List<string> FileList = GetFileListToDirectory(FilePath, Job.WhiteFileExt);
                    foreach (string File in FileList)
                    {
                        TaskData Data = new TaskData(TaskData.TaskNameList.Upload, File, DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"), GetFileSize(File));
                        TaskSQL.Insert(Data);
                        log.Debug(Data.FilePath);
                    }
                    return true;
                }
                else if (CheckWhiteFileList(FileName))
                {
                    TaskData Data = new TaskData(TaskData.TaskNameList.Upload, FilePath, DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"), GetFileSize(FilePath));
                    TaskSQL.Insert(Data);
                    log.Debug(Data.FilePath);
                    return true;
                }
            }

            return false;
        }
        private bool DeleteBackup(string FilePath)
        {
            string FileName = MainData.GetFileName(FilePath);

            if (!FilePathException(FilePath))
            {
                if (CheckWhiteFileList(FileName))
                {
                    TaskData Data = new TaskData(TaskData.TaskNameList.Delete, FilePath,
                                                DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"));
                    TaskSQL.Insert(Data);
                    log.Debug(Data.FilePath);
                    return true;
                }
            }
            return false;
        }
        private bool RenameBackup(string FilePath, string NewFilePath)
        {
            //string FileName = MainData.GetFileName(FilePath);

            string NewFileName = MainData.GetFileName(NewFilePath);

            if (!FilePathException(NewFileName))
            {
                if (CheckIsFolder(NewFilePath))
                {
                    FilePath += "\\";
                    NewFilePath += "\\";
                    TaskData Data = new TaskData(TaskData.TaskNameList.Rename, FilePath,
                                                DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"), NewFilePath);
                    TaskSQL.Insert(Data);
                    log.Debug($"{Data.FilePath} => {Data.NewFilePath}");
                }
                else if (CheckWhiteFileList(NewFileName))
                {
                    TaskData Data = new TaskData(TaskData.TaskNameList.Rename, FilePath,
                                                DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"), NewFilePath);
                    TaskSQL.Insert(Data);
                    log.Debug($"{Data.FilePath} => {Data.NewFilePath}");
                    return true;
                }
            }

            return true;
        }
        #endregion  Sql Task Upload

        #region ETC
        private string ChangeHardLinkDriveName(string FilePath)
        {
            //\Device\LanmanRedirector\;<drive letter>:<logon-session id>\<server>\<share>\<path>"
            //Get <drive letter>:<path>
            if (FilePath.StartsWith(Net))
            {
                string from = FilePath.Replace(Net, ""); //delete [\Device\LanmanRedirector\;]
                string VolumName = from.Substring(0, 2); //Get <drive letter>:

                string[] result = from.Split('\\'); //cut <drive letter>:<logon-session id>, <server>, <share>, <path> ....

                string NewFilePath = VolumName;
                for (int i = 3; i < result.Length; i++)//Get <path>....
                {
                    NewFilePath += "\\" + result[i];
                }
                return NewFilePath;
            }
            else return FilePath;
        }
        private bool WriteFileEventCheck(string FilePath, long FileSize)
        {
            bool exist = false;
            for (int i = 0; i < NotifyWriteFileList.Count; i++)
            {
                if (NotifyWriteFileList[i].FilePath == FilePath)
                {
                    NotifyWriteFileList[i].SizeUpdate(FileSize);
                    return false;
                }
            }
            if (!exist) NotifyWriteFileList.Add(new WriteFileInfo(FilePath, FileSize));
            return exist;
        }
        #endregion ETC
        #region Filter Notify Event

        private void NotifyRenameOrMoveFile(object Sender, CbmonitorNotifyRenameOrMoveFileEventArgs args)
        {
            string FilePath = ChangeHardLinkDriveName(args.FileName);
            string NewFilePath = ChangeHardLinkDriveName(args.NewFileName);

            FilterEventHandler.EventList Event = FilterEventHandler.FindSaveByRenameEvent(Job.Path, FilePath, NewFilePath);

            string Msg;
            switch (Event)
            {
                case FilterEventHandler.EventList.Rename     : RenameBackup(FilePath, NewFilePath); Msg = $"Rename      : {FilePath} => {NewFilePath}"; break;
                case FilterEventHandler.EventList.SaveFile   : CreateBackup(FilePath);              Msg = $"SaveFile    : {FilePath}"; break;
                case FilterEventHandler.EventList.SaveNewFile: CreateBackup(NewFilePath);           Msg = $"SaveNewFile : {FilePath}"; break;
                case FilterEventHandler.EventList.Delete     : DeleteBackup(FilePath);              Msg = $"Delete      : {FilePath}"; break;
                case FilterEventHandler.EventList.None       : 
                default: return; 
            }

            log.Debug(Msg);
        }
        private void NotifyCreateFile(object Sender, CbmonitorNotifyCreateFileEventArgs args)
        {
            if (!CheckIsFolder(args.FileName))
            {
                log.Debug(args.FileName);
                CreateBackup(args.FileName);
            }
        }
        #region File Write Event

        private void NotifySetFileSize(object Sender, CbmonitorNotifySetFileSizeEventArgs args)
        {
            log.Debug(args.FileName);

            WriteFileEventCheck(args.FileName, args.Size);
        }
        private void NotifyGetFileSizes(object Sender, CbmonitorNotifyGetFileSizesEventArgs args)
        {
            log.Debug(args.FileName);

            string ProcessName = ((Cbmonitor)Sender).GetOriginatorProcessName();
            if (ProcessName.EndsWith(MSMPENG, StringComparison.OrdinalIgnoreCase)) WriteFileEventCheck(args.FileName, args.Size);
        }
        private void NotifyWriteFile(object Sender, CbmonitorNotifyWriteFileEventArgs args)
        {
            log.Debug(args.FileName);

            foreach (var item in NotifyWriteFileList)
            {
                if (item.FilePath == args.FileName)
                {
                    long FileSize = args.Position + args.BytesWritten;
                    if (item.Size <= FileSize) CreateBackup(args.FileName);
                }
            }

        }
        #endregion File Write Event

        #region File Delete Event
        private void NotifyDeleteFile(object Sender, CbmonitorNotifyDeleteFileEventArgs args)
        {
            log.Debug(args.FileName);
            DeleteBackup(args.FileName);
        }

        private void NotifyCanFileBeDeleted(object Sender, CbmonitorNotifyCanFileBeDeletedEventArgs args)
        {
            if (!args.CanDelete)
            {
                int index = DeleteStacks.IndexOf(args.FileName);
                if (index < 0)
                {
                    //log.Debug("Add to delete list : {2}", args.FileName);
                    DeleteStacks.Add(args.FileName);
                }
                else
                {
                    log.Debug($"delete : {args.FileName}");
                    DeleteStacks.RemoveAt(index);
                    DeleteBackup(args.FileName);
                }
            }
        }
        #endregion File Delete Event
        #endregion Filter Notify Event
    }
}
