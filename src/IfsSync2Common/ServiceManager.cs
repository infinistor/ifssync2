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
using System.Diagnostics;

namespace IfsSync2Common
{
	public static class ServiceManager
	{
		const string LINUX_SERVICE_PATH = "/etc/systemd/system/";
		const string MAC_SERVICE_PATH = "/Library/LaunchDaemons/";
		const string UNSUPPORTED_PLATFORM = "Unsupported platform";
		const string SERVICE_FOR_WINDOWS = "sc";
		const string SERVICE_FOR_LINUX = "systemctl";
		const string SERVICE_FOR_MAC = "launchctl";

		#region Public methods
		public static void RegisterService(string serviceName, string executablePath)
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
					RegisterServiceWindows(serviceName, executablePath);
					break;
				case PlatformID.Unix:
					RegisterServiceLinux(serviceName, executablePath);
					break;
				case PlatformID.MacOSX:
					RegisterServiceMac(serviceName, executablePath);
					break;
				default:
					throw new NotSupportedException(UNSUPPORTED_PLATFORM);
			}
		}
		public static void UnregisterService(string serviceName)
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
					UnregisterServiceWindows(serviceName);
					break;
				case PlatformID.Unix:
					UnregisterServiceLinux(serviceName);
					break;
				case PlatformID.MacOSX:
					UnregisterServiceMac(serviceName);
					break;
				default:
					throw new NotSupportedException(UNSUPPORTED_PLATFORM);
			}
		}
		public static void StartService(string serviceName)
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
					{
						using var process = new Process();
						process.StartInfo.FileName = SERVICE_FOR_WINDOWS;
						process.StartInfo.Arguments = $"start {serviceName}";
						process.Start();
						break;
					}
				case PlatformID.Unix:
					{
						using var process = new Process();
						process.StartInfo.FileName = SERVICE_FOR_LINUX;
						process.StartInfo.Arguments = $"start {serviceName}";
						process.Start();
						break;
					}
				case PlatformID.MacOSX:
					{
						using var process = new Process();
						process.StartInfo.FileName = SERVICE_FOR_MAC;
						process.StartInfo.Arguments = $"start {serviceName}";
						process.Start();
						break;
					}
				default:
					throw new NotSupportedException(UNSUPPORTED_PLATFORM);
			}
		}
		public static void StopService(string serviceName)
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
					{
						using var process = new Process();
						process.StartInfo.FileName = SERVICE_FOR_WINDOWS;
						process.StartInfo.Arguments = $"stop {serviceName}";
						process.Start();
						break;
					}
				case PlatformID.Unix:
					{
						using var process = new Process();
						process.StartInfo.FileName = SERVICE_FOR_LINUX;
						process.StartInfo.Arguments = $"stop {serviceName}";
						process.Start();
						break;
					}
				case PlatformID.MacOSX:
					{
						using var process = new Process();
						process.StartInfo.FileName = SERVICE_FOR_MAC;
						process.StartInfo.Arguments = $"stop {serviceName}";
						process.Start();
						break;
					}
				default:
					throw new NotSupportedException(UNSUPPORTED_PLATFORM);
			}
		}
		#endregion

		#region Private methods
		private static void RegisterServiceWindows(string serviceName, string executablePath)
		{
			using var process = new Process();
			process.StartInfo.FileName = SERVICE_FOR_WINDOWS;
			process.StartInfo.Arguments = $"create {serviceName} binPath= \"{executablePath}\" start= auto";
			process.Start();
		}

		private static void RegisterServiceLinux(string serviceName, string executablePath)
		{
			string serviceFileContent = $"[Unit]\nDescription={serviceName}\n\n[Service]\nExecStart={executablePath}\n\n[Install]\nWantedBy=multi-user.target";
			System.IO.File.WriteAllText($"{LINUX_SERVICE_PATH}{serviceName}.service", serviceFileContent);

			using var process = new Process();
			process.StartInfo.FileName = SERVICE_FOR_LINUX;
			process.StartInfo.Arguments = $"enable {serviceName}";
			process.Start();
		}

		private static void RegisterServiceMac(string serviceName, string executablePath)
		{
			string plistContent = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\">\n<dict>\n<key>Label</key>\n<string>{serviceName}</string>\n<key>ProgramArguments</key>\n<array>\n<string>{executablePath}</string>\n</array>\n<key>RunAtLoad</key>\n<true/>\n</dict>\n</plist>";
			System.IO.File.WriteAllText($"{MAC_SERVICE_PATH}{serviceName}.plist", plistContent);

			using var process = new Process();
			process.StartInfo.FileName = SERVICE_FOR_MAC;
			process.StartInfo.Arguments = $"load -w /Library/LaunchDaemons/{serviceName}.plist";
			process.Start();
		}

		private static void UnregisterServiceWindows(string serviceName)
		{
			using var process = new Process();
			process.StartInfo.FileName = SERVICE_FOR_WINDOWS;
			process.StartInfo.Arguments = $"delete {serviceName}";
			process.Start();
		}

		private static void UnregisterServiceLinux(string serviceName)
		{
			using var process = new Process();
			process.StartInfo.FileName = SERVICE_FOR_LINUX;
			process.StartInfo.Arguments = $"disable {serviceName}";
			process.Start();

			System.IO.File.Delete($"{LINUX_SERVICE_PATH}{serviceName}.service");
		}

		private static void UnregisterServiceMac(string serviceName)
		{
			using var process = new Process();
			process.StartInfo.FileName = SERVICE_FOR_MAC;
			process.StartInfo.Arguments = $"unload -w /Library/LaunchDaemons/{serviceName}.plist";
			process.Start();

			System.IO.File.Delete($"{MAC_SERVICE_PATH}{serviceName}.plist");
		}
		#endregion
	}
} 