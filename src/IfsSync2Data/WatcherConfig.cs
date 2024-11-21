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

namespace IfsSync2Data
{
	public class WatcherConfig
	{
		#region Define
		const string WATCHER_CHECK_DELAY = "WatcherCheckDelay";
		const string WATCHER_IP = "IP";
		const string WATCHER_PORT = "Port";
		const string WATCHER_PC_NAME = "PcName";
		const string WATCHER_EMAIL = "Email";
		const string ROOT_PATH = "RootPath";
		const int DEFAULT_WATCHER_CHECK_DELAY = 5 * 60 * 1000; // 5 min
		#endregion

		readonly RegistryKey _watcherConfigKey = null;

#pragma warning disable CA1416
		public int WatcherCheckDelay
		{
			get => int.TryParse(_watcherConfigKey.GetValue(WATCHER_CHECK_DELAY).ToString(), out int value) ? value : DEFAULT_WATCHER_CHECK_DELAY;
			set => _watcherConfigKey.SetValue(WATCHER_CHECK_DELAY, value, RegistryValueKind.DWord);
		}
		public string IP
		{
			get => _watcherConfigKey.GetValue(WATCHER_IP).ToString();
			set => _watcherConfigKey.SetValue(WATCHER_IP, value, RegistryValueKind.String);
		}
		public string Port
		{
			get => _watcherConfigKey.GetValue(WATCHER_PORT).ToString();
			set => _watcherConfigKey.SetValue(WATCHER_PORT, value, RegistryValueKind.String);
		}
		public string PcName
		{
			get => _watcherConfigKey.GetValue(WATCHER_PC_NAME).ToString();
			set => _watcherConfigKey.SetValue(WATCHER_PC_NAME, value, RegistryValueKind.String);
		}
		public string Email
		{
			get => _watcherConfigKey.GetValue(WATCHER_EMAIL).ToString();
			set => _watcherConfigKey.SetValue(WATCHER_EMAIL, value, RegistryValueKind.String);
		}
		public string RootPath
		{
			get => _watcherConfigKey.GetValue(ROOT_PATH).ToString();
			set => _watcherConfigKey.SetValue(ROOT_PATH, value, RegistryValueKind.String);
		}
		public WatcherConfig(bool write = false)
		{
			_watcherConfigKey = Registry.LocalMachine.OpenSubKey(MainData.WATCHER_CONFIG_PATH, write);
			if (_watcherConfigKey == null)
			{
				_watcherConfigKey = Registry.LocalMachine.CreateSubKey(MainData.WATCHER_CONFIG_PATH);
				_watcherConfigKey.SetValue(WATCHER_CHECK_DELAY, DEFAULT_WATCHER_CHECK_DELAY, RegistryValueKind.DWord);
				_watcherConfigKey.SetValue(WATCHER_IP, "", RegistryValueKind.String);
				_watcherConfigKey.SetValue(WATCHER_PORT, "", RegistryValueKind.String);
				_watcherConfigKey.SetValue(WATCHER_PC_NAME, "", RegistryValueKind.String);
				_watcherConfigKey.SetValue(WATCHER_EMAIL, "", RegistryValueKind.String);
				_watcherConfigKey.SetValue(ROOT_PATH, "", RegistryValueKind.String);
			}
		}

		public void Close() { _watcherConfigKey?.Close(); }

		public void Delete()
		{
			_watcherConfigKey.DeleteSubKeyTree(MainData.WATCHER_CONFIG_PATH);
		}

#pragma warning restore CA1416
	}
}
