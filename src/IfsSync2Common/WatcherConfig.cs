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
	public class WatcherConfig
	{
		readonly RegistryKey _watcherConfigKey;

#pragma warning disable CA1416
		public int WatcherCheckDelay
		{
			get => RegistryUtility.GetIntValue(_watcherConfigKey, IfsSync2Constants.REG_KEY_WATCHER_CHECK_DELAY);
			set => _watcherConfigKey.SetValue(IfsSync2Constants.REG_KEY_WATCHER_CHECK_DELAY, value, RegistryValueKind.DWord);
		}
		public string IP
		{
			get => RegistryUtility.GetStringValue(_watcherConfigKey, IfsSync2Constants.REG_KEY_WATCHER_IP);
			set => _watcherConfigKey.SetValue(IfsSync2Constants.REG_KEY_WATCHER_IP, value, RegistryValueKind.String);
		}
		public string Port
		{
			get => RegistryUtility.GetStringValue(_watcherConfigKey, IfsSync2Constants.REG_KEY_WATCHER_PORT);
			set => _watcherConfigKey.SetValue(IfsSync2Constants.REG_KEY_WATCHER_PORT, value, RegistryValueKind.String);
		}
		public string PcName
		{
			get => RegistryUtility.GetStringValue(_watcherConfigKey, IfsSync2Constants.REG_KEY_WATCHER_PC_NAME);
			set => _watcherConfigKey.SetValue(IfsSync2Constants.REG_KEY_WATCHER_PC_NAME, value, RegistryValueKind.String);
		}
		public string Email
		{
			get => RegistryUtility.GetStringValue(_watcherConfigKey, IfsSync2Constants.REG_KEY_WATCHER_EMAIL);
			set => _watcherConfigKey.SetValue(IfsSync2Constants.REG_KEY_WATCHER_EMAIL, value, RegistryValueKind.String);
		}
		public string RootPath
		{
			get => RegistryUtility.GetStringValue(_watcherConfigKey, IfsSync2Constants.REG_KEY_ROOT_PATH);
			set => _watcherConfigKey.SetValue(IfsSync2Constants.REG_KEY_ROOT_PATH, value, RegistryValueKind.String);
		}
		public WatcherConfig(bool write = false)
		{
			var temp = Registry.LocalMachine.OpenSubKey(IfsSync2Constants.WATCHER_CONFIG_PATH, write);
			if (temp == null)
			{
				temp = Registry.LocalMachine.CreateSubKey(IfsSync2Constants.WATCHER_CONFIG_PATH);
				temp.SetValue(IfsSync2Constants.REG_KEY_WATCHER_CHECK_DELAY, IfsSync2Constants.DEFAULT_WATCHER_CHECK_DELAY, RegistryValueKind.DWord);
				temp.SetValue(IfsSync2Constants.REG_KEY_WATCHER_IP, "", RegistryValueKind.String);
				temp.SetValue(IfsSync2Constants.REG_KEY_WATCHER_PORT, "", RegistryValueKind.String);
				temp.SetValue(IfsSync2Constants.REG_KEY_WATCHER_PC_NAME, "", RegistryValueKind.String);
				temp.SetValue(IfsSync2Constants.REG_KEY_WATCHER_EMAIL, "", RegistryValueKind.String);
				temp.SetValue(IfsSync2Constants.REG_KEY_ROOT_PATH, "", RegistryValueKind.String);
			}
			_watcherConfigKey = temp;
		}

		public void Close() { _watcherConfigKey?.Close(); }

		public void Delete()
		{
			_watcherConfigKey.DeleteSubKeyTree(IfsSync2Constants.WATCHER_CONFIG_PATH);
		}

#pragma warning restore CA1416
	}
}