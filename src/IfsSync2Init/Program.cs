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
using Microsoft.Win32.TaskScheduler;
using System;
using IfsSync2Data;
using System.Diagnostics;
using Microsoft.Win32;
using callback.CBFSFilter;
using Mono.Options;
using System.Collections.Generic;

namespace IfsSync2Init
{
	class Program : Process
	{
		public enum MenuList
		{
			Help, Install, Uninstall
		}
		static int Main(string[] args)
		{
			Console.WriteLine("Main Start");

			var IP = string.Empty;
			var Port = string.Empty;
			var Host = string.Empty;
			var TargetPath = string.Empty;

			var Menu = MenuList.Help;

			var Options = new OptionSet {
				{ "h|?|help", v => Menu = MenuList.Help},
				{ "i|install", v => Menu = MenuList.Install },
				{ "u|uninstall", v => Menu = MenuList.Uninstall},
				{ "p|path=", v => TargetPath = v},
				{ "host=", v => Host = v },
				{ "ip=", v => IP = v },
				{ "port=", v => Port = v },
			};

			try
			{
				List<string> Extra = Options.Parse(args);

				foreach (var Option in Extra) Console.WriteLine($"{Option} : invalid. {Options[Option].Description}");

				if (Extra.Count > 0) return -1;
			}
			catch (OptionException e)
			{
				// output some error message
				Console.WriteLine(e.Message);
				Console.WriteLine($"{e.OptionName} : {Options[e.OptionName.Replace("--", "")].Description}");
				return -127;
			}
			if (!TargetPath.EndsWith(System.IO.Path.DirectorySeparatorChar)) TargetPath += System.IO.Path.DirectorySeparatorChar;

			switch (Menu)
			{
				case MenuList.Install:
					Console.WriteLine("Install Start");
					Create(IP, Port, TargetPath);
					Console.WriteLine("Install End!");
					return 0;
				case MenuList.Uninstall:
					Console.WriteLine("Uninstall Start");
					Delete();
					Console.WriteLine("Uninstall End!");
					return 0;
				case MenuList.Help:
					Options.WriteOptionDescriptions(Console.Out);
					return 0;
				default:
					Options.WriteOptionDescriptions(Console.Out);
					return -1;
			}
		}

		static void Create(string ip, string port, string targetPath)
		{
			Console.WriteLine("CBFS Install Start.");
			CBFSInstall(targetPath);
			Console.WriteLine("CBFS Install End.");

			//Create RegistryKey
			Console.WriteLine("Registry Setting.");
			var WatcherConfigs = new WatcherConfig(true);
			var FilterConfigs = new FilterConfig(true);
			var SenderConfigs = new SenderConfig(true);
			var TrayIconConfigs = new TrayIconConfig(true);

			WatcherConfigs.IP = ip;
			WatcherConfigs.Port = port;
			WatcherConfigs.RootPath = targetPath;
			FilterConfigs.RootPath = targetPath;
			SenderConfigs.RootPath = targetPath;
			TrayIconConfigs.RootPath = targetPath;
			TrayIconConfigs.IconPath = MainData.CreateIconFilePath(targetPath, MainData.ICON_FILE_NAME);
			WatcherConfigs.Close();
			FilterConfigs.Close();
			SenderConfigs.Close();
			TrayIconConfigs.Close();

			if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
			{
				var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
				using var netDriverSetting = baseKey.OpenSubKey(MainData.NET_DRIVER_REGISTRY_PATH, true);
				netDriverSetting.SetValue(MainData.NET_DRIVER_ENABLE_LINKED_CONNECTIONS, 1, RegistryValueKind.DWord);
				Console.WriteLine("Registry Setting End!");
			}

			Console.WriteLine("Task Schedule Add.");
			SetTask(MainData.FILTER_NAME, targetPath, false);
			SetTask(MainData.SENDER_NAME, targetPath);
			SetTask(MainData.TRAY_ICON_NAME, targetPath, false);
			Console.WriteLine("Task Schedule Add End!");

			Console.WriteLine("Service Create.");
			ServiceManager.RegisterService(MainData.WATCHER_SERVICE_NAME, MainData.CreateExeFilePath(targetPath, MainData.WATCHER_SERVICE_NAME));
			Console.WriteLine("Service Start.");
			ServiceManager.StartService(MainData.WATCHER_SERVICE_NAME);
			Console.WriteLine("Service Setting End!");

		}
		static void Delete()
		{
			Console.WriteLine("Task Schedule Delete.");
			DelTask(MainData.FILTER_NAME);
			DelTask(MainData.SENDER_NAME);
			DelTask(MainData.TRAY_ICON_NAME);
			Console.WriteLine("Task Schedule Delete End!");

			Console.WriteLine("ProcessKill.");
			ProcessKill(MainData.UI_NAME);
			ProcessKill(MainData.FILTER_NAME);
			ProcessKill(MainData.SENDER_NAME);
			ProcessKill(MainData.TRAY_ICON_NAME);
			Console.WriteLine("ProcessKill End!");

			Console.WriteLine("Registry Delete.");
			if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
				try
				{
					Registry.LocalMachine.DeleteSubKeyTree(MainData.REGISTRY_ROOT);
				}
				catch (Exception e)
				{
					Console.WriteLine(e.Message);
				}
			Console.WriteLine("Registry Delete End!");

			Console.WriteLine("Service Stop.");
			ServiceManager.StopService(MainData.WATCHER_SERVICE_NAME);
			Console.WriteLine("Service Delete.");
			ServiceManager.UnregisterService(MainData.WATCHER_SERVICE_NAME);
			Console.WriteLine("Service Delete End!");

			Console.WriteLine("CBFS Uninstall Start.");
			CBFSUninstall();
			Console.WriteLine("CBFS Uninstall End.");
		}

		private static void SetTask(string ScheduleName, string RootPath, bool AC = true)
		{
			using var taskService = new TaskService();
			try
			{
				Task task = taskService.GetTask(ScheduleName);
				if (task.Enabled) return;
				else taskService.RootFolder.DeleteTask(ScheduleName);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}

			var taskDefinition = taskService.NewTask();

			var DefaultRepetition = new RepetitionPattern(TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));

			// 일반
			taskDefinition.RegistrationInfo.Description = "Scheduler";

			// 관리자 권한으로 실행
			taskDefinition.Principal.UserId = "SYSTEM";
			taskDefinition.Principal.LogonType = TaskLogonType.ServiceAccount;
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
			var FilePath = MainData.CreateExeFilePath(RootPath, ScheduleName);
			taskDefinition.Actions.Add(new ExecAction(FilePath, RootPath, null));

			// 등록
			taskService.RootFolder.RegisterTaskDefinition(ScheduleName, taskDefinition);
		}
		private static void DelTask(string ScheduleName)
		{
			using var taskService = new TaskService();
			try
			{
				taskService.RootFolder.DeleteTask(ScheduleName);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		private static void ProcessKill(string ProcessName)
		{
			var ProcessList = GetProcessesByName(ProcessName);

			if (ProcessList.Length > 0)
			{
				foreach (var process in ProcessList)
				{
					process.Kill();
					Console.WriteLine("{0} : is Kill", process.ProcessName);
				}
			}
		}

		#region CBFS
		private static void CBFSInstall(string targetPath)
		{
			var path = targetPath + MainData.FILTER_DRIVE_PATH;
			Console.WriteLine(path);

			var mFilter = new Cbfilter(MainData.RUNTIME_LICENSE_KEY);
			if (!CBFSInstallCheck(mFilter))
			{
				try
				{
					bool Reboot = mFilter.Install(path, MainData.mGuid, null, Constants.FS_FILTER_MODE_MINIFILTER, MainData.ALTITUDE_FAKE_VALUE_FOR_DEBUG, 0);

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
			var mFilter = new Cbfilter(MainData.RUNTIME_LICENSE_KEY);

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
		#endregion
	}
}
