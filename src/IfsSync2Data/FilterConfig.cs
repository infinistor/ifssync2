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
    class FilterConfig
    {
        private static readonly string FILTER_CHECK_DELAY = "FilterCheckDelay";
        private const string ROOT_PATH = "RootPath";
        /***************************************************************/
        private static readonly int DEFAULT_FILTER_CHECK_DELAY = 5000;
        /***************************************************************/
        private readonly RegistryKey FilterConfigKey = null;

        public int FilterCheckDelay
        {
            get
            {
                int value = 0;
                try { value = Convert.ToInt32(FilterConfigKey.GetValue(FILTER_CHECK_DELAY)); } catch { }
                return value;
            }
            set
            {
                FilterConfigKey.SetValue(FILTER_CHECK_DELAY, value, RegistryValueKind.DWord);
            }
        }

        public string RootPath
        {
            get { return FilterConfigKey.GetValue(ROOT_PATH).ToString(); }
            set { FilterConfigKey.SetValue(ROOT_PATH, value, RegistryValueKind.String); }
        }
        public bool Alive
        {
            get
            {
                int value = MainData.MY_FALSE;
                try { value = Convert.ToInt32(FilterConfigKey.GetValue(MainData.ALIVE_CHECK)); } catch { }

                if (value == MainData.MY_FALSE) return false;
                else return true;
            }
            set
            {
                int temp = MainData.MY_FALSE;
                if (value) temp = MainData.MY_TRUE;

                FilterConfigKey.SetValue(MainData.ALIVE_CHECK, temp, RegistryValueKind.DWord);
            }
        }
        public FilterConfig(bool Write = false)
        {
            FilterConfigKey = Registry.LocalMachine.OpenSubKey(MainData.FILTER_CONFIG_PATH, Write);
            if (FilterConfigKey == null)
            {
                FilterConfigKey = Registry.LocalMachine.CreateSubKey(MainData.FILTER_CONFIG_PATH);
                FilterConfigKey.SetValue(FILTER_CHECK_DELAY, DEFAULT_FILTER_CHECK_DELAY, RegistryValueKind.DWord);
                FilterConfigKey.SetValue(ROOT_PATH, "", RegistryValueKind.String);
                FilterConfigKey.SetValue(MainData.ALIVE_CHECK, MainData.MY_FALSE, RegistryValueKind.DWord);
            }
        }
        public void Close() { if(FilterConfigKey != null) FilterConfigKey.Close(); }
        public void Delete()
        {
            FilterConfigKey.DeleteSubKeyTree(MainData.FILTER_CONFIG_PATH);
        }
    }
}
