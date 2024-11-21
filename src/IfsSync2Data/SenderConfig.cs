﻿/*
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
	public class SenderConfig
	{
		#region Define
		const string SENDER_FETCH_COUNT = "SenderFetchCount";
		const string SENDER_DELAY = "SenderDelay";
		const string SENDER_CHECK_DELAY = "SenderCheckDelay";
		const string SENDER_STOP = "SenderStop";
		const string ROOT_PATH = "RootPath";
		const int DEFAULT_FETCH_COUNT = 1000; //5sec
		const int DEFAULT_SENDER_DELAY = 5 * 1000; //5sec
		const int DEFAULT_SENDER_CHECK_DELAY = 5 * 1000; //5 sec
		#endregion

		readonly RegistryKey SenderConfigKey = null;

#pragma warning disable CA1416
		public int FetchCount
		{
			get => int.TryParse(SenderConfigKey.GetValue(SENDER_FETCH_COUNT).ToString(), out int value) ? value : 0;
			set => SenderConfigKey.SetValue(SENDER_FETCH_COUNT, value, RegistryValueKind.DWord);
		}
		public int SenderDelay
		{
			get => int.TryParse(SenderConfigKey.GetValue(SENDER_DELAY).ToString(), out int value) ? value : 0;
			set => SenderConfigKey.SetValue(SENDER_DELAY, value, RegistryValueKind.DWord);
		}
		public int SenderCheckDelay
		{
			get => int.TryParse(SenderConfigKey.GetValue(SENDER_CHECK_DELAY).ToString(), out int value) ? value : 0;
			set => SenderConfigKey.SetValue(SENDER_CHECK_DELAY, value, RegistryValueKind.DWord);
		}
		public string RootPath
		{
			get => SenderConfigKey.GetValue(ROOT_PATH).ToString();
			set => SenderConfigKey.SetValue(ROOT_PATH, value, RegistryValueKind.String);
		}
		public bool Stop
		{
			get => int.TryParse(SenderConfigKey.GetValue(SENDER_STOP).ToString(), out int value) && value == MainData.MY_TRUE;
			set => SenderConfigKey.SetValue(SENDER_STOP, value ? MainData.MY_TRUE : MainData.MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Alive
		{
			get => int.TryParse(SenderConfigKey.GetValue(MainData.ALIVE_CHECK).ToString(), out int value) && value == MainData.MY_TRUE;
			set => SenderConfigKey.SetValue(MainData.ALIVE_CHECK, value ? MainData.MY_TRUE : MainData.MY_FALSE, RegistryValueKind.DWord);
		}
		public SenderConfig(bool write = false)
		{
			SenderConfigKey = Registry.LocalMachine.OpenSubKey(MainData.SENDER_CONFIG_PATH, write);
			if (SenderConfigKey == null)
			{
				SenderConfigKey = Registry.LocalMachine.CreateSubKey(MainData.SENDER_CONFIG_PATH);

				SenderConfigKey.SetValue(SENDER_FETCH_COUNT, DEFAULT_FETCH_COUNT, RegistryValueKind.DWord);
				SenderConfigKey.SetValue(SENDER_DELAY, DEFAULT_SENDER_DELAY, RegistryValueKind.DWord);
				SenderConfigKey.SetValue(SENDER_CHECK_DELAY, DEFAULT_SENDER_CHECK_DELAY, RegistryValueKind.DWord);
				SenderConfigKey.SetValue(ROOT_PATH, "", RegistryValueKind.String);
				SenderConfigKey.SetValue(SENDER_STOP, MainData.MY_FALSE, RegistryValueKind.DWord);
				SenderConfigKey.SetValue(MainData.ALIVE_CHECK, MainData.MY_FALSE, RegistryValueKind.DWord);
			}
		}

		public void Close() { SenderConfigKey?.Close(); }
		public void Delete()
		{
			SenderConfigKey.DeleteSubKeyTree(MainData.SENDER_CONFIG_PATH);
		}
#pragma warning restore CA1416
	}
}
