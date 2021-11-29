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
using Microsoft.Win32.TaskScheduler;
using System;
using IfsSync2Data;
using System.Diagnostics;
using Gnu.Getopt;
using Microsoft.Win32;
using callback.CBFSFilter;

namespace IfsSync2Init
{
    class Program : Process
    {

        static void Main(string[] args)
        {
            Console.WriteLine("Main Start");

            string IP = string.Empty;
            string Port = string.Empty;
            string TargetPath = string.Empty;

            Getopt g = new Getopt("IfsSync2Init", args, "?hds:i:p:", null);
            bool CreateFlag = false;
            bool DeleteFlag = false;

            int c;
            while ((c = g.getopt()) != -1)
            {
                switch (c)
                {
                    case '?':
                    case 'h':
                        Help(); return;
                    case 's':
                        {
                            TargetPath = g.Optarg;
                            if (string.IsNullOrWhiteSpace(TargetPath))
                            {
                                Console.WriteLine("[-s] URL is empty!");
                                return;
                            }
                            else
                            {
                                CreateFlag = true;
                                if (!TargetPath.EndsWith("\\")) TargetPath += "\\";
                            }
                            break;
                        }
                    case 'u':
                        {
                            IP = g.Optarg;
                            if (string.IsNullOrWhiteSpace(IP))
                            {
                                Console.WriteLine("[-s] IP is empty!");
                                return;
                            }
                            else CreateFlag = true;
                        break;
                        }
                    case 'p':
                        {
                            Port = g.Optarg;
                            if (string.IsNullOrWhiteSpace(Port))
                            {
                                Console.WriteLine("[-s] Port is empty!");
                                return;
                            }
                            else CreateFlag = true;
                            break;
                        }
                    case 'd':
                        DeleteFlag = true;
                        break;
                }
            }

            if (CreateFlag)
            {
                Console.WriteLine("Install Start");
                Create(IP, Port, TargetPath);
                Console.WriteLine("Install End!");
            }
            else if(DeleteFlag)
            {
                Console.WriteLine("Uninstall Start");
                Delete();
                Console.WriteLine("Uninstall End!");
            }

            Console.WriteLine("Main End!");
        }
        
        static void Help()
        {
            string msg = "-?              Program Usage Manual.\n" +
                         "-h              Program Usage Manual.\n" +
                         "-u   :(string)  Program install Path.\n" +
                         "-s   :(string)  IfsSync2 Server URL.\n" +
                         "-d              Program uninstall.\n";

            Console.WriteLine(msg);
        }
        static void Create(string IP, string Port, string TargetPath)
        {
            Console.WriteLine("CBFS Install Start.");
            CBFSInstall();
            Console.WriteLine("CBFS Install End.");

            //Create RegistryKey
            Console.WriteLine("Registry Setting.");
            WatcherConfig WatcherConfigs = new WatcherConfig(true);
            FilterConfig FilterConfigs = new FilterConfig(true);
            SenderConfig SenderConfigs = new SenderConfig(true);
            TrayIconConfig TrayIconConfigs = new TrayIconConfig(true);

            WatcherConfigs.IP = IP;
            WatcherConfigs.Port = Port;
            WatcherConfigs.RootPath = TargetPath;
            FilterConfigs.RootPath = TargetPath;
            SenderConfigs.RootPath = TargetPath;
            TrayIconConfigs.RootPath = TargetPath;
            TrayIconConfigs.IconPath = MainData.CreateIconPath(TargetPath, MainData.ICON_FILE_NAME);
            WatcherConfigs.Close();
            FilterConfigs.Close();
            SenderConfigs.Close();
            TrayIconConfigs.Close();

            RegistryKey NetDriverSetting = Registry.LocalMachine.OpenSubKey(MainData.NETDIRVER_REGISTRY_PATH, true);
            NetDriverSetting.SetValue(MainData.NETDRIVER_ENABLELINKEDCONNECTIONS, 1, RegistryValueKind.DWord);
            NetDriverSetting.Close();
            Console.WriteLine("Registry Setting End!");

            Console.WriteLine("Task Schedule Add.");
            SetTask(MainData.FILTER_NAME, TargetPath, false);
            SetTask(MainData.SENDER_NAME, TargetPath);
            SetTask(MainData.TRAYICON_NAME, TargetPath, false);
            Console.WriteLine("Task Schedule Add End!");

            string CmdCreate = string.Format("sc create \"{0}\" binpath= \"{1}\" start= auto",
                MainData.WATCHER_SERVICE_NAME, MainData.CreateFilePath(TargetPath, MainData.WATCHER_SERVICE_NAME));
            string CmdStart = string.Format("sc start {0}", MainData.WATCHER_SERVICE_NAME);

            Console.WriteLine("Service Create.");
            CMDExecute(CmdCreate);//Service 생성
            Console.WriteLine("Service Start.");
            CMDExecute(CmdStart); //Service 시작
            Console.WriteLine("Service Setting End!");

        }
        static void Delete()
        {
            Console.WriteLine("Task Schedule Delete.");
            DelTask(MainData.FILTER_NAME);
            DelTask(MainData.SENDER_NAME);
            DelTask(MainData.TRAYICON_NAME);
            Console.WriteLine("Task Schedule Delete End!");

            Console.WriteLine("ProcessKill.");
            ProcessKill(MainData.UI_NAME);
            ProcessKill(MainData.FILTER_NAME);
            ProcessKill(MainData.SENDER_NAME);
            ProcessKill(MainData.TRAYICON_NAME);
            Console.WriteLine("ProcessKill End!");

            Console.WriteLine("Registry Delete.");
            try { Registry.LocalMachine.DeleteSubKeyTree(MainData.REGISTRY_ROOT); } catch { }
            Console.WriteLine("Registry Delete End!");


            Console.WriteLine("Service Stop.");
            string CmdStop = string.Format("sc stop \"{0}\"", MainData.WATCHER_SERVICE_NAME);
            CMDExecute(CmdStop);
            Console.WriteLine("Service Delete.");
            string CmdDelete = string.Format("sc delete \"{0}\"", MainData.WATCHER_SERVICE_NAME);
            CMDExecute(CmdDelete);
            Console.WriteLine("Service Delete End!");

            Console.WriteLine("CBFS Uninstall Start.");
            CBFSUninstall();
            Console.WriteLine("CBFS Unistall End.");
        }


        private static void SetTask(string ScheduleName, string RootPath, bool AC = true)
        {
            using (TaskService taskService = new TaskService())
            {
                try
                {
                    Task task = taskService.GetTask(ScheduleName);
                    if (task.Enabled) return;
                    else taskService.RootFolder.DeleteTask(ScheduleName);
                }
                catch { }


                TaskDefinition taskDefinition = taskService.NewTask();

                RepetitionPattern DefaultRepetition = new RepetitionPattern(TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));

                // 일반
                taskDefinition.RegistrationInfo.Description = "Scheduler";

                //최고 권한 실행
                taskDefinition.Principal.RunLevel = TaskRunLevel.Highest;

                taskDefinition.Triggers.Add(new RegistrationTrigger() { Repetition = DefaultRepetition, Delay = TimeSpan.FromMinutes(1) });
                taskDefinition.Triggers.Add(new BootTrigger() { Repetition = DefaultRepetition, Delay = TimeSpan.FromMinutes(1) });
                taskDefinition.Triggers.Add(new LogonTrigger() { Repetition = DefaultRepetition, Delay = TimeSpan.FromMinutes(1) });

                // 조건 > 전원
                // 컴퓨터의 AC 전원이 켜져 있는 경우에만 작업 시작
                taskDefinition.Settings.DisallowStartIfOnBatteries = AC;

                // 조건 > 네트워크
                // 다음 네트워크 연결을 사용할 수 있는 경우에만 시작
                taskDefinition.Settings.RunOnlyIfNetworkAvailable = true;

                //설정 > 예약시간을 놓친 경우 가능한 대로 빨리 작업시작
                taskDefinition.Settings.StartWhenAvailable = true;

                taskDefinition.Settings.ExecutionTimeLimit = TimeSpan.FromDays(0);

                // 실행
                string FilePath = MainData.CreateFilePath(RootPath, ScheduleName);
                taskDefinition.Actions.Add(new ExecAction(FilePath, RootPath, null));

                // 등록
                taskService.RootFolder.RegisterTaskDefinition(ScheduleName, taskDefinition);
            }
        }
        private static void DelTask(string ScheduleName)
        {
            using (TaskService taskService = new TaskService())
            {
                try { taskService.RootFolder.DeleteTask(ScheduleName); } catch { }
            }
        }
        private static void CMDExecute(string CMD)
        {
            ProcessStartInfo info = new ProcessStartInfo();
            Process process = new Process();

            info.FileName = "cmd.exe";

            info.CreateNoWindow = false;
            info.UseShellExecute = false;

            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.RedirectStandardInput = true;
            process.StartInfo = info;
            process.Start();

            process.StandardInput.WriteLine(CMD);
            process.StandardInput.Close();
            string Result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.Close();

            Console.WriteLine(Result);

        }
        private static void ProcessKill(string ProcessName)
        {
            Process[] ProcessList = Process.GetProcessesByName(ProcessName);

            if (ProcessList.Length > 0)
            {
                foreach (var process in ProcessList)
                {
                    process.Kill();
                    Console.WriteLine("{0} : is Kill", process.ProcessName);
                }
            }
        }

        private static void CBFSInstall()
        {
            Cbfilter mFilter = new Cbfilter(MainData.RUNTIME_LICENSE_KEY);
            if (!CBFSInstallCheck(mFilter))
            {
                try
                {
                    bool Reboot = mFilter.Install(MainData.FILTER_DRIVE_PATH, MainData.mGuid, null,
                        Constants.FS_FILTER_MODE_MINIFILTER, MainData.ALTITUDE_FAKE_VALUE_FOR_DEBUG, 0);

                    if (Reboot)
                        Console.WriteLine("Reboot the computer for the changes to take affect");
                    else
                        Console.WriteLine("Driver installed successfully");
                }
                catch (CBFSFilterCbfilterException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        private static void CBFSUninstall()
        {
            Cbfilter mFilter = new Cbfilter(MainData.RUNTIME_LICENSE_KEY);

            if (CBFSInstallCheck(mFilter))
            {
                try
                {
                    bool Reboot = mFilter.Uninstall(MainData.FILTER_DRIVE_PATH, MainData.mGuid, null, 0);

                    if (Reboot)
                        Console.WriteLine("Reboot the computer for the changes to take affect");
                    else
                        Console.WriteLine("Driver installed successfully");
                }
                catch (CBFSFilterCbfilterException e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public static bool CBFSInstallCheck(Cbfilter mFilter)
        {
            int moduleStatus = mFilter.GetDriverStatus(MainData.mGuid);

            ulong moduleVersion = (ulong)mFilter.GetDriverVersion(MainData.mGuid);

            uint versionHigh = (uint)(moduleVersion >> 32);
            uint versionLow = (uint)(moduleVersion & 0xFFFFFFFF);


            if (moduleStatus != 0)
            {
                Console.WriteLine("CBFS Driver version: {0}.{1}.{2}.{3}",
                    versionHigh >> 16, versionHigh & 0xFFFF, versionLow >> 16, versionLow & 0xFFFF);
                return true;
            }
            else
            {
                Console.WriteLine("CBFS Driver is not install");
                return false;
            }
        }
    }
}
