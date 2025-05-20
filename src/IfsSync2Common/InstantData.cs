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
	public class InstantData
	{
		readonly RegistryKey _instantKey;

#pragma warning disable CA1416
		public bool Analysis
		{
			get => RegistryUtility.GetBoolValue(_instantKey, IfsSync2Constants.REG_KEY_ANALYSIS);
			set => _instantKey.SetValue(IfsSync2Constants.REG_KEY_ANALYSIS, value ? IfsSync2Constants.MY_TRUE : IfsSync2Constants.MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Running
		{
			get => RegistryUtility.GetBoolValue(_instantKey, IfsSync2Constants.REG_KEY_RUNNING);
			set => _instantKey.SetValue(IfsSync2Constants.REG_KEY_RUNNING, value ? IfsSync2Constants.MY_TRUE : IfsSync2Constants.MY_FALSE, RegistryValueKind.DWord);
		}

		public long Total
		{
			get => RegistryUtility.GetLongValue(_instantKey, IfsSync2Constants.REG_KEY_TOTAL_COUNT);
			set => _instantKey.SetValue(IfsSync2Constants.REG_KEY_TOTAL_COUNT, value, RegistryValueKind.QWord);
		}
		public long Upload
		{
			get => RegistryUtility.GetLongValue(_instantKey, IfsSync2Constants.REG_KEY_UPLOAD_COUNT);
			set => _instantKey.SetValue(IfsSync2Constants.REG_KEY_UPLOAD_COUNT, value, RegistryValueKind.QWord);
		}
		public long Percent
		{
			get => RegistryUtility.GetLongValue(_instantKey, IfsSync2Constants.REG_KEY_PERCENT_COUNT);
			set => _instantKey.SetValue(IfsSync2Constants.REG_KEY_PERCENT_COUNT, value, RegistryValueKind.QWord);
		}

		public InstantData()
		{
			var temp = Registry.LocalMachine.OpenSubKey(IfsSync2Constants.INSTANT_REGISTRY_ROOT_NAME, true);
			if (temp == null)
			{
				temp = Registry.LocalMachine.CreateSubKey(IfsSync2Constants.INSTANT_REGISTRY_ROOT_NAME, true);
				temp.SetValue(IfsSync2Constants.REG_KEY_ANALYSIS, IfsSync2Constants.MY_TRUE, RegistryValueKind.DWord);
				temp.SetValue(IfsSync2Constants.REG_KEY_TOTAL_COUNT, 0, RegistryValueKind.QWord);
				temp.SetValue(IfsSync2Constants.REG_KEY_UPLOAD_COUNT, 0, RegistryValueKind.QWord);
				temp.SetValue(IfsSync2Constants.REG_KEY_PERCENT_COUNT, 0, RegistryValueKind.QWord);
			}
			_instantKey = temp;
		}
#pragma warning restore CA1416

		public void Init()
		{
			Analysis = false;
			Running = false;
			Total = 0;
			Upload = 0;
			Percent = 0;
		}

		public void Clear()
		{
			Total = Upload = Percent = 0;
		}
	}
}
