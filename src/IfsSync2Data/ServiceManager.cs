using System;
using System.Diagnostics;

namespace IfsSync2Data
{
	public static class ServiceManager
	{
		const string LINUX_SERVICE_PATH = "/etc/systemd/system/";
		const string MAC_SERVICE_PATH = "/Library/LaunchDaemons/";

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
					throw new NotSupportedException("Unsupported platform");
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
					throw new NotSupportedException("Unsupported platform");
			}
		}
		public static void StartService(string serviceName)
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
					{
						using var process = new Process();
						process.StartInfo.FileName = "sc";
						process.StartInfo.Arguments = $"start {serviceName}";
						process.Start();
						break;
					}
				case PlatformID.Unix:
					{
						using var process = new Process();
						process.StartInfo.FileName = "systemctl";
						process.StartInfo.Arguments = $"start {serviceName}";
						process.Start();
						break;
					}
				case PlatformID.MacOSX:
					{
						using var process = new Process();
						process.StartInfo.FileName = "launchctl";
						process.StartInfo.Arguments = $"start {serviceName}";
						process.Start();
						break;
					}
				default:
					throw new NotSupportedException("Unsupported platform");
			}
		}
		public static void StopService(string serviceName)
		{
			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
					{
						using var process = new Process();
						process.StartInfo.FileName = "sc";
						process.StartInfo.Arguments = $"stop {serviceName}";
						process.Start();
						break;
					}
				case PlatformID.Unix:
					{
						using var process = new Process();
						process.StartInfo.FileName = "systemctl";
						process.StartInfo.Arguments = $"stop {serviceName}";
						process.Start();
						break;
					}
				case PlatformID.MacOSX:
					{
						using var process = new Process();
						process.StartInfo.FileName = "launchctl";
						process.StartInfo.Arguments = $"stop {serviceName}";
						process.Start();
						break;
					}
				default:
					throw new NotSupportedException("Unsupported platform");
			}
		}
		#endregion

		#region Private methods
		private static void RegisterServiceWindows(string serviceName, string executablePath)
		{
			using var process = new Process();
			process.StartInfo.FileName = "sc";
			process.StartInfo.Arguments = $"create {serviceName} binPath= \"{executablePath}\" start= auto";
			process.Start();
		}

		private static void RegisterServiceLinux(string serviceName, string executablePath)
		{
			string serviceFileContent = $"[Unit]\nDescription={serviceName}\n\n[Service]\nExecStart={executablePath}\n\n[Install]\nWantedBy=multi-user.target";
			System.IO.File.WriteAllText($"{LINUX_SERVICE_PATH}{serviceName}.service", serviceFileContent);

			using var process = new Process();
			process.StartInfo.FileName = "systemctl";
			process.StartInfo.Arguments = $"enable {serviceName}";
			process.Start();
		}

		private static void RegisterServiceMac(string serviceName, string executablePath)
		{
			string plistContent = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n<plist version=\"1.0\">\n<dict>\n<key>Label</key>\n<string>{serviceName}</string>\n<key>ProgramArguments</key>\n<array>\n<string>{executablePath}</string>\n</array>\n<key>RunAtLoad</key>\n<true/>\n</dict>\n</plist>";
			System.IO.File.WriteAllText($"{MAC_SERVICE_PATH}{serviceName}.plist", plistContent);

			using var process = new Process();
			process.StartInfo.FileName = "launchctl";
			process.StartInfo.Arguments = $"load -w /Library/LaunchDaemons/{serviceName}.plist";
			process.Start();
		}

		private static void UnregisterServiceWindows(string serviceName)
		{
			using var process = new Process();
			process.StartInfo.FileName = "sc";
			process.StartInfo.Arguments = $"delete {serviceName}";
			process.Start();
		}

		private static void UnregisterServiceLinux(string serviceName)
		{
			using var process = new Process();
			process.StartInfo.FileName = "systemctl";
			process.StartInfo.Arguments = $"disable {serviceName}";
			process.Start();

			System.IO.File.Delete($"{LINUX_SERVICE_PATH}{serviceName}.service");
		}

		private static void UnregisterServiceMac(string serviceName)
		{
			using var process = new Process();
			process.StartInfo.FileName = "launchctl";
			process.StartInfo.Arguments = $"unload -w /Library/LaunchDaemons/{serviceName}.plist";
			process.Start();

			System.IO.File.Delete($"{MAC_SERVICE_PATH}{serviceName}.plist");
		}
		#endregion
	}
}