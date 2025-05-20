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
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using IfsSync2Common;

namespace IfsSync2UI
{
	#region FileIcon
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
	public struct SHFileInfo
	{
		public IntPtr hIcon;

		public int iIcon;

		public uint dwAttributes;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szDisplayName;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
		public string szTypeName;
	};
	public enum IconSize : short
	{
		Small,
		Large
	}
	public enum ItemType : short
	{
		Folder,
		File
	}
	public enum ItemState : short
	{
		Undefined,
		Open,
		Close
	}
	#endregion

	static class Utility
	{
		static readonly char[] SpecialCharacters = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

		public static void ErrorMessageBox(string Msg, string Title)
		{
			MessageBox.Show(Msg, Title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
		}

		public static void ServerCertificateValidationCallback()
		{
			if (ServicePointManager.ServerCertificateValidationCallback == null)
			{
				ServicePointManager.ServerCertificateValidationCallback +=
				delegate (object obj, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) { return true; };
			}
		}

		public static bool SpecialCharactersErrorCheck(string FileName)
		{
			foreach (char Special in SpecialCharacters) if (FileName.Contains(Special)) return true;
			return false;
		}
		/************************* Get Global User **********************************/

		static readonly string STR_IP = "ip";
		static readonly string STR_HOSTNAME = "hostname";
		static readonly string STR_PORT = "port";
		static readonly string STR_MAC = "mac";
		static readonly string STR_OS = "os";
		static readonly string STR_GROUP = "group";
		static readonly string STR_PC_NAME = "pcName";


		static readonly int PORT_NUMBER = 58443;

		static readonly string STR_ERR_MSG = "err_msg";
		static readonly string STR_RET = "ret";

		static readonly string STR_USERID = "userid";
		static readonly string STR_S3PROXY = "s3proxy";
		static readonly string STR_ACCESS_KEY = "access_key";
		static readonly string STR_ACCESS_SECRET = "access_secret";
		static readonly string STR_TENANT_KEY = "tenant";

		static string GetMacAddress()
		{
			return NetworkInterface.GetAllNetworkInterfaces()[0].GetPhysicalAddress().ToString();
		}
		static string GetIPAddress()
		{
			string IP = string.Empty;
			IPAddress[] Host = Dns.GetHostAddresses(Dns.GetHostName());
			foreach (var Item in Host)
			{
				if (Item.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				{
					IP = Item.ToString();
					break;
				}
			}
			return IP;
		}
		public static UserData GetGlobalUser(string url, string pcName)
		{
			string URL;
			if (url.EndsWith('/')) URL = url + IfsSync2Constants.WATCHER_SERVICE_GET_USER;
			else URL = url + "/" + IfsSync2Constants.WATCHER_SERVICE_GET_USER;

			OperatingSystem os = Environment.OSVersion;
			string data = $"{{\"{STR_IP}\":\"{GetIPAddress()}\", " +
						$"\"{STR_HOSTNAME}\":\"{Dns.GetHostName()}\", " +
						$"\"{STR_PORT}\":\"{PORT_NUMBER}\", " +
						$"\"{STR_MAC}\":\"{GetMacAddress()}\", " +
						$"\"{STR_OS}\":\"{os.Platform.ToString().ToUpper()[..3]}\", " +
						$"\"{STR_GROUP}\":\"0\"," +
						$"\"{STR_PC_NAME}\":\"{pcName}\"}}";

			using var client = new HttpClient();
			client.Timeout = TimeSpan.FromMilliseconds(IfsSync2Constants.CURL_TIMEOUT_DELAY);

			var content = new StringContent(data, Encoding.UTF8, IfsSync2Constants.CURL_CONTENT_TYPE);
			var response = client.PostAsync(URL, content).Result;
			var responseText = response.Content.ReadAsStringAsync().Result;

			JObject UserObj = JObject.Parse(responseText);

			if (!int.TryParse(UserObj[STR_RET].ToString(), out int ret))
			{
				throw new ArgumentException(STR_RET + " is Not int");
			}
			if (ret != 0)
			{
				string ErrorMsg = UserObj[STR_ERR_MSG].ToString();
				throw new InvalidOperationException(ErrorMsg);
			}

			return new UserData()
			{
				URL = UserObj[STR_S3PROXY].ToString(),
				UserName = UserObj[STR_USERID].ToString(),
				AccessKey = UserObj[STR_ACCESS_KEY].ToString(),
				SecretKey = UserObj[STR_ACCESS_SECRET].ToString(),
				StorageName = UserObj[STR_TENANT_KEY].ToString(),
				Debug = false
			};
		}
		public static UserData GetGlobalUser(string _URL, string IP, string HostName, string MAC, string PcName)
		{
			string URL;
			if (_URL.EndsWith('/')) URL = _URL + IfsSync2Constants.WATCHER_SERVICE_GET_USER;
			else URL = _URL + "/" + IfsSync2Constants.WATCHER_SERVICE_GET_USER;

			OperatingSystem os = Environment.OSVersion;
			string data = $"{{\"{STR_IP}\":\"{IP}\", " +
							$"\"{STR_HOSTNAME}\":\"{HostName}\", " +
							$"\"{STR_PORT}\":\"{PORT_NUMBER}\", " +
							$"\"{STR_MAC}\":\"{MAC}\", " +
							$"\"{STR_OS}\":\"{os.Platform.ToString().ToUpper()[..3]}\", " +
							$"\"{STR_GROUP}\":\"0\"," +
							$"\"{STR_PC_NAME}\":\"{PcName}\"}}";

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(IfsSync2Constants.CURL_CONTENT_TYPE));
			client.Timeout = TimeSpan.FromMilliseconds(IfsSync2Constants.CURL_TIMEOUT_DELAY);

			var content = new StringContent(data, Encoding.UTF8, IfsSync2Constants.CURL_CONTENT_TYPE);
			var response = client.PostAsync(URL, content).Result;
			var responseText = response.Content.ReadAsStringAsync().Result;

			JObject UserObj = JObject.Parse(responseText);

			if (!int.TryParse(UserObj[STR_RET].ToString(), out int ret))
			{
				throw new ArgumentException(STR_RET + " is Not int");
			}
			if (ret != 0)
			{
				string ErrorMsg = UserObj[STR_ERR_MSG].ToString();
				throw new InvalidOperationException(ErrorMsg);
			}

			return new UserData()
			{
				URL = UserObj[STR_S3PROXY].ToString(),
				UserName = UserObj[STR_USERID].ToString(),
				AccessKey = UserObj[STR_ACCESS_KEY].ToString(),
				SecretKey = UserObj[STR_ACCESS_SECRET].ToString(),
				StorageName = UserObj[STR_TENANT_KEY].ToString(),
				Debug = false
			};
		}
		/*****************************Get Icon***************************************/
		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFileInfo psfi, uint cbFileInfo, uint uFlags);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DestroyIcon(IntPtr hIcon);

		public const uint ICON = 0x000000100;
		public const uint USE_FILE_ATTRIBUTES = 0x000000010;
		public const uint OPEN_ICON = 0x000000002;
		public const uint SMALL_ICON = 0x000000001;
		public const uint LARGE_ICON = 0x000000000;
		public const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
		public const uint FILE_ATTRIBUTE_FILE = 0x00000100;

		[DllImport("gdi32.dll", SetLastError = true)]
		static extern bool DeleteObject(IntPtr hObject);

		[SupportedOSPlatform("windows10.0")]
		public static ImageSource IconToImageSource(this Icon icon)
		{
			ImageSource wpfBitmap = null;
			Bitmap bitmap = icon.ToBitmap();
			IntPtr hBitmap = bitmap.GetHbitmap();

			wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
				hBitmap,
				IntPtr.Zero,
				Int32Rect.Empty,
				BitmapSizeOptions.FromEmptyOptions());

			if (!DeleteObject(hBitmap))
			{
				throw new Win32Exception();
			}
			return wpfBitmap;
		}

		[SupportedOSPlatform("windows10.0")]
		public static ImageSource GetIconImageSource(string path)
		{
			Icon defaultIcon = GetIcon(path, IconSize.Small, ItemState.Close);
			return IconToImageSource(defaultIcon);
		}

		[SupportedOSPlatform("windows10.0")]
		public static Icon GetIcon(string path, IconSize size, ItemState state)
		{
			var flags = ICON | USE_FILE_ATTRIBUTES;

			if (Equals(state, ItemState.Open)) { flags += OPEN_ICON; }
			if (Equals(size, IconSize.Small)) { flags += SMALL_ICON; }
			else { flags += LARGE_ICON; }

			var res = SHGetFileInfo(path, FILE_ATTRIBUTE_DIRECTORY, out var shfi, (uint)Marshal.SizeOf<SHFileInfo>(), flags);
			if (Equals(res, IntPtr.Zero)) throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());

			try
			{
				return (Icon)Icon.FromHandle(shfi.hIcon).Clone();
			}
			finally
			{
				DestroyIcon(shfi.hIcon);
			}
		}
	}
}

