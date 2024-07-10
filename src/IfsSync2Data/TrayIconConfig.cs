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
using Microsoft.Win32;
using System;

namespace IfsSync2Data
{
	class TrayIconConfig
	{
		const string REMAINING = "Remaining";
		const string REMAINING_SIZE = "RemainingSize";
		const string UPLOAD_COUNT = "UploadCount";
		const string UPLOAD_FAIL_COUNT = "UploadFailCount";
		const string FILE_SIZE = "FileSize";
		const string DELAY = "UpdateDelay";
		const string ICON_PATH = "IconPath";
		const string ROOT_PATH = "RootPath";
		static readonly int DEFAULT_DELAY = 5000;
		readonly RegistryKey TrayIconKey = null;

		public long Remaining
		{
			get
			{
				long value = 0;
				try { value = Convert.ToInt64(TrayIconKey.GetValue(REMAINING)); } catch { }
				return value;
			}
			set
			{
				TrayIconKey.SetValue(REMAINING, value, RegistryValueKind.QWord);
			}
		}
		public long RemainingSize
		{
			get
			{
				long value = 0;
				try { value = Convert.ToInt64(TrayIconKey.GetValue(REMAINING_SIZE)); } catch { }
				return value;
			}
			set
			{
				TrayIconKey.SetValue(REMAINING_SIZE, value, RegistryValueKind.QWord);
			}
		}
		public long UploadCount
		{
			get
			{
				long value = 0;
				try { value = Convert.ToInt64(TrayIconKey.GetValue(UPLOAD_COUNT)); } catch { }
				return value;
			}
			set
			{
				TrayIconKey.SetValue(UPLOAD_COUNT, value, RegistryValueKind.QWord);
			}
		}
		public long UploadFailCount
		{
			get
			{
				long value = 0;
				try { value = Convert.ToInt64(TrayIconKey.GetValue(UPLOAD_FAIL_COUNT)); } catch { }
				return value;
			}
			set
			{
				TrayIconKey.SetValue(UPLOAD_FAIL_COUNT, value, RegistryValueKind.QWord);
			}
		}
		public long FileSize
		{
			get
			{
				long value = 0;
				try { value = Convert.ToInt64(TrayIconKey.GetValue(FILE_SIZE)); } catch { }
				return value;
			}
			set
			{
				TrayIconKey.SetValue(FILE_SIZE, value, RegistryValueKind.QWord);
			}
		}
		public int Delay
		{
			get
			{
				int value = 0;
				try { value = Convert.ToInt32(TrayIconKey.GetValue(DELAY)); } catch { }
				return value;
			}
			set
			{
				TrayIconKey.SetValue(DELAY, value, RegistryValueKind.DWord);
			}
		}
		public string IconPath
		{
			get { return TrayIconKey.GetValue(ICON_PATH).ToString(); }
			set { TrayIconKey.SetValue(ICON_PATH, value, RegistryValueKind.String); }
		}
		public string RootPath
		{
			get { return TrayIconKey.GetValue(ROOT_PATH).ToString(); }
			set { TrayIconKey.SetValue(ROOT_PATH, value, RegistryValueKind.String); }
		}
		public void AddFileSize(int _FileSize)
		{
			if (UploadCount >= int.MaxValue / 2) UploadCount = 0;
			if (FileSize >= int.MaxValue / 2) FileSize = 0;
			UploadCount++;
			FileSize += _FileSize;
		}
		public void Init()
		{
			UploadCount = 0;
			FileSize = 0;
		}
		public TrayIconConfig(bool Write = false)
		{
			TrayIconKey = Registry.LocalMachine.OpenSubKey(MainData.TRAY_ICON_CONFIG_PATH, Write);
			if (TrayIconKey == null)
			{
				TrayIconKey = Registry.LocalMachine.CreateSubKey(MainData.TRAY_ICON_CONFIG_PATH);

				TrayIconKey.SetValue(REMAINING, 0, RegistryValueKind.QWord);
				TrayIconKey.SetValue(REMAINING_SIZE, 0, RegistryValueKind.QWord);
				TrayIconKey.SetValue(UPLOAD_COUNT, 0, RegistryValueKind.QWord);
				TrayIconKey.SetValue(FILE_SIZE, 0, RegistryValueKind.QWord);
				TrayIconKey.SetValue(UPLOAD_FAIL_COUNT, 0, RegistryValueKind.QWord);
				TrayIconKey.SetValue(DELAY, DEFAULT_DELAY, RegistryValueKind.DWord);
				TrayIconKey.SetValue(ICON_PATH, "", RegistryValueKind.String);
				TrayIconKey.SetValue(ROOT_PATH, "", RegistryValueKind.String);
			}
		}

		public void Clear()
		{
			Remaining = 0;
			UploadCount = 0;
			UploadFailCount = 0;
			FileSize = 0;
		}
		public void Close() { TrayIconKey?.Close(); }
		public void Delete()
		{
			TrayIconKey.DeleteSubKeyTree(MainData.TRAY_ICON_CONFIG_PATH);
		}
	}
}
