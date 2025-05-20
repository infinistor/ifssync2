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
	public class FilterConfig
	{
		#region Define
		static readonly string FILTER_CHECK_DELAY = "FilterCheckDelay";
		const string ROOT_PATH = "RootPath";
		static readonly int DEFAULT_FILTER_CHECK_DELAY = 5000;
		#endregion

		readonly RegistryKey _filterConfigKey;

#pragma warning disable CA1416
		public int FilterCheckDelay
		{
			get => RegistryUtility.GetIntValue(_filterConfigKey, FILTER_CHECK_DELAY);
			set => _filterConfigKey.SetValue(FILTER_CHECK_DELAY, value, RegistryValueKind.DWord);
		}

		public string RootPath
		{
			get => RegistryUtility.GetStringValue(_filterConfigKey, ROOT_PATH);
			set => _filterConfigKey.SetValue(ROOT_PATH, value, RegistryValueKind.String);
		}
		public bool Alive
		{
			get => RegistryUtility.GetBoolValue(_filterConfigKey, IfsSync2Constants.ALIVE_CHECK);
			set => _filterConfigKey.SetValue(IfsSync2Constants.ALIVE_CHECK, value ? IfsSync2Constants.MY_TRUE : IfsSync2Constants.MY_FALSE, RegistryValueKind.DWord);
		}
		public FilterConfig(bool Write = false)
		{
			var temp = Registry.LocalMachine.OpenSubKey(IfsSync2Constants.FILTER_CONFIG_PATH, Write);
			if (temp == null)
			{
				temp = Registry.LocalMachine.CreateSubKey(IfsSync2Constants.FILTER_CONFIG_PATH);
				temp.SetValue(FILTER_CHECK_DELAY, DEFAULT_FILTER_CHECK_DELAY, RegistryValueKind.DWord);
				temp.SetValue(ROOT_PATH, "", RegistryValueKind.String);
				temp.SetValue(IfsSync2Constants.ALIVE_CHECK, IfsSync2Constants.MY_FALSE, RegistryValueKind.DWord);
			}

			_filterConfigKey = temp;
		}

		public void Close() { _filterConfigKey?.Close(); }
		public void Delete()
		{
			_filterConfigKey.DeleteSubKeyTree(IfsSync2Constants.FILTER_CONFIG_PATH);
		}
#pragma warning restore CA1416
	}
}