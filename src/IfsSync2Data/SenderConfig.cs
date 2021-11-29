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
    class SenderConfig
    {
        private static readonly string SENDER_FETCH_COUNT = "SenderFetchCount";
        private static readonly string SENDER_DELAY = "SenderDelay";
        private static readonly string SENDER_CHECK_DELAY = "SenderCheckDelay";
        private static readonly string SENDER_STOP = "SenderStop";
        private const string ROOT_PATH = "RootPath";
        /***************************************************************/
        private static readonly int DEFAULT_FETCH_COUNT = 1000; //5sec
        private static readonly int DEFAULT_SENDER_DELAY = 5 * 1000; //5sec
        private static readonly int DEFAULT_SENDER_CHECK_DELAY = 5 * 1000; //5 sec
        /***************************************************************/

        private readonly RegistryKey SenderConfigKey = null;

        public int FetchCount
        {
            get
            {
                int value = 0;
                try { value = Convert.ToInt32(SenderConfigKey.GetValue(SENDER_FETCH_COUNT)); } catch { }
                return value;
            }
            set
            {
                SenderConfigKey.SetValue(SENDER_FETCH_COUNT, value, RegistryValueKind.DWord);
            }
        }
        public int SenderDelay
        {
            get
            {
                int value = 0;
                try { value = Convert.ToInt32(SenderConfigKey.GetValue(SENDER_DELAY)); } catch { }
                return value;
            }
            set
            {
                SenderConfigKey.SetValue(SENDER_DELAY, value, RegistryValueKind.DWord);
            }
        }
        public int SenderCheckDelay
        {
            get
            {
                int value = 0;
                try { value = Convert.ToInt32(SenderConfigKey.GetValue(SENDER_CHECK_DELAY)); } catch { }
                return value;
            }
            set
            {
                SenderConfigKey.SetValue(SENDER_CHECK_DELAY, value, RegistryValueKind.DWord);
            }
        }
        public string RootPath
        {
            get { return SenderConfigKey.GetValue(ROOT_PATH).ToString(); }
            set { SenderConfigKey.SetValue(ROOT_PATH, value, RegistryValueKind.String); }
        }
        public bool Stop
        {
            get
            {
                int value = MainData.MY_FALSE;
                try { value = Convert.ToInt32(SenderConfigKey.GetValue(SENDER_STOP)); } catch { }

                if (value == MainData.MY_FALSE) return false;
                else return true;
            }
            set
            {
                int temp = MainData.MY_FALSE;
                if (value) temp = MainData.MY_TRUE;

                SenderConfigKey.SetValue(SENDER_STOP, temp, RegistryValueKind.DWord);
            }
        }
        public bool Alive
        {
            get
            {
                int value = MainData.MY_FALSE;
                try { value = Convert.ToInt32(SenderConfigKey.GetValue(MainData.ALIVE_CHECK)); } catch { }

                if (value == MainData.MY_FALSE) return false;
                else return true;
            }
            set
            {
                int temp = MainData.MY_FALSE;
                if (value) temp = MainData.MY_TRUE;

                SenderConfigKey.SetValue(MainData.ALIVE_CHECK, temp, RegistryValueKind.DWord);
            }
        }
        public SenderConfig(bool Write = false)
        {
            SenderConfigKey = Registry.LocalMachine.OpenSubKey(MainData.SENDER_CONFIG_PATH, Write);
            if(SenderConfigKey == null)
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

        public void Close() { if(SenderConfigKey != null) SenderConfigKey.Close(); }
        public void Delete()
        {
            SenderConfigKey.DeleteSubKeyTree(MainData.SENDER_CONFIG_PATH);
        }
    }
}
