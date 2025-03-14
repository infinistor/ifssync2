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

namespace IfsSync2Data
{
	public class InstantData
	{
		#region Define
		const string ANALYSIS = "Analysis";
		const string RUNNING = "Running";
		const string TOTAL = "TotalCount";
		const string UPLOAD = "UploadCount";
		const string PERCENT = "PercentCount";
		#endregion

		readonly RegistryKey _instantKey = null;

#pragma warning disable CA1416
		public bool Analysis
		{
			get => int.TryParse(_instantKey.GetValue(ANALYSIS).ToString(), out int value) && value == MainData.MY_TRUE;
			set => _instantKey.SetValue(ANALYSIS, value ? MainData.MY_TRUE : MainData.MY_FALSE, RegistryValueKind.DWord);
		}
		public bool Running
		{
			get => int.TryParse(_instantKey.GetValue(RUNNING).ToString(), out int value) && value == MainData.MY_TRUE;
			set => _instantKey.SetValue(RUNNING, value ? MainData.MY_TRUE : MainData.MY_FALSE, RegistryValueKind.DWord);
		}

		public long Total
		{
			get => long.TryParse(_instantKey.GetValue(TOTAL).ToString(), out long value) ? value : 0;
			set => _instantKey.SetValue(TOTAL, value, RegistryValueKind.QWord);
		}
		public long Upload
		{
			get => long.TryParse(_instantKey.GetValue(UPLOAD).ToString(), out long value) ? value : 0;
			set => _instantKey.SetValue(UPLOAD, value, RegistryValueKind.QWord);
		}
		public long Percent
		{
			get => long.TryParse(_instantKey.GetValue(PERCENT).ToString(), out long value) ? value : 0;
			set => _instantKey.SetValue(PERCENT, value, RegistryValueKind.QWord);
		}

		public InstantData()
		{
			_instantKey = Registry.LocalMachine.OpenSubKey(MainData.INSTANT_REGISTRY_ROOT_NAME, true);
			if (_instantKey == null)
			{
				_instantKey = Registry.LocalMachine.CreateSubKey(MainData.INSTANT_REGISTRY_ROOT_NAME, true);
				_instantKey.SetValue(ANALYSIS, MainData.MY_TRUE, RegistryValueKind.DWord);
				_instantKey.SetValue(TOTAL, 0, RegistryValueKind.QWord);
				_instantKey.SetValue(UPLOAD, 0, RegistryValueKind.QWord);
				_instantKey.SetValue(PERCENT, 0, RegistryValueKind.QWord);
			}
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
