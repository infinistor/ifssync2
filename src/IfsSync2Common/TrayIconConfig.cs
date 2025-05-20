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
using Microsoft.Win32;

namespace IfsSync2Common
{
	public class TrayIconConfig
	{
		readonly RegistryKey _trayIconKey;

#pragma warning disable CA1416
		public long Remaining
		{
			get => RegistryUtility.GetLongValue(_trayIconKey, IfsSync2Constants.REG_KEY_REMAINING);
			set => _trayIconKey.SetValue(IfsSync2Constants.REG_KEY_REMAINING, value, RegistryValueKind.QWord);
		}
		public long RemainingSize
		{
			get => RegistryUtility.GetLongValue(_trayIconKey, IfsSync2Constants.REG_KEY_REMAINING_SIZE);
			set => _trayIconKey.SetValue(IfsSync2Constants.REG_KEY_REMAINING_SIZE, value, RegistryValueKind.QWord);
		}
		public long UploadCount
		{
			get => RegistryUtility.GetLongValue(_trayIconKey, IfsSync2Constants.REG_KEY_UPLOAD_COUNT);
			set => _trayIconKey.SetValue(IfsSync2Constants.REG_KEY_UPLOAD_COUNT, value, RegistryValueKind.QWord);
		}
		public long UploadFailCount
		{
			get => RegistryUtility.GetLongValue(_trayIconKey, IfsSync2Constants.REG_KEY_UPLOAD_FAIL_COUNT);
			set => _trayIconKey.SetValue(IfsSync2Constants.REG_KEY_UPLOAD_FAIL_COUNT, value, RegistryValueKind.QWord);
		}
		public long FileSize
		{
			get => RegistryUtility.GetLongValue(_trayIconKey, IfsSync2Constants.REG_KEY_FILE_SIZE);
			set => _trayIconKey.SetValue(IfsSync2Constants.REG_KEY_FILE_SIZE, value, RegistryValueKind.QWord);
		}
		public int Delay
		{
			get => RegistryUtility.GetIntValue(_trayIconKey, IfsSync2Constants.REG_KEY_DELAY);
			set => _trayIconKey.SetValue(IfsSync2Constants.REG_KEY_DELAY, value, RegistryValueKind.DWord);
		}
		public string IconPath
		{
			get => RegistryUtility.GetStringValue(_trayIconKey, IfsSync2Constants.REG_KEY_ICON_PATH);
			set => _trayIconKey.SetValue(IfsSync2Constants.REG_KEY_ICON_PATH, value, RegistryValueKind.String);
		}
		public string RootPath
		{
			get => RegistryUtility.GetStringValue(_trayIconKey, IfsSync2Constants.REG_KEY_ROOT_PATH);
			set => _trayIconKey.SetValue(IfsSync2Constants.REG_KEY_ROOT_PATH, value, RegistryValueKind.String);
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
		public TrayIconConfig(bool write = false)
		{
			var temp = Registry.LocalMachine.OpenSubKey(IfsSync2Constants.TRAY_ICON_CONFIG_PATH, write);
			if (temp == null)
			{
				temp = Registry.LocalMachine.CreateSubKey(IfsSync2Constants.TRAY_ICON_CONFIG_PATH);

				temp.SetValue(IfsSync2Constants.REG_KEY_REMAINING, 0, RegistryValueKind.QWord);
				temp.SetValue(IfsSync2Constants.REG_KEY_REMAINING_SIZE, 0, RegistryValueKind.QWord);
				temp.SetValue(IfsSync2Constants.REG_KEY_UPLOAD_COUNT, 0, RegistryValueKind.QWord);
				temp.SetValue(IfsSync2Constants.REG_KEY_FILE_SIZE, 0, RegistryValueKind.QWord);
				temp.SetValue(IfsSync2Constants.REG_KEY_UPLOAD_FAIL_COUNT, 0, RegistryValueKind.QWord);
				temp.SetValue(IfsSync2Constants.REG_KEY_DELAY, IfsSync2Constants.DEFAULT_STATUS_CHECK_DELAY, RegistryValueKind.DWord);
				temp.SetValue(IfsSync2Constants.REG_KEY_ICON_PATH, "", RegistryValueKind.String);
				temp.SetValue(IfsSync2Constants.REG_KEY_ROOT_PATH, "", RegistryValueKind.String);
			}
			_trayIconKey = temp;
		}

		public void Clear()
		{
			Remaining = 0;
			UploadCount = 0;
			UploadFailCount = 0;
			FileSize = 0;
		}
		public void Close() { _trayIconKey?.Close(); }
		public void Delete()
		{
			_trayIconKey.DeleteSubKeyTree(IfsSync2Constants.TRAY_ICON_CONFIG_PATH);
		}
#pragma warning restore CA1416
	}
}