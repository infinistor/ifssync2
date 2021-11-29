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
    class WatcherConfig
    {
        private const string WATCHER_CHECK_DELAY = "WatcherCheckDelay";
        private const string WATCHER_IP = "IP";
        private const string WATCHER_PORT = "Port";
        private const string WATCHER_PCNAME = "PcName";
        private const string WATCHER_EMAIL = "Email";
        private const string ROOT_PATH = "RootPath";
        /***************************************************************/
        private const int DEFAULT_WATCHER_CHECK_DELAY = 5 * 60 * 1000; // 5 min
        /***************************************************************/
        private readonly RegistryKey WatcherConfigKey = null;

        public int WatcherCheckDelay
        {
            get
            {
                int value = 0;
                try { value = Convert.ToInt32(WatcherConfigKey.GetValue(WATCHER_CHECK_DELAY)); } catch { }
                return value;
            }
            set
            {
                WatcherConfigKey.SetValue(WATCHER_CHECK_DELAY, value, RegistryValueKind.DWord);
            }
        }
        public string IP
        {
            get { return WatcherConfigKey.GetValue(WATCHER_IP).ToString(); }
            set { WatcherConfigKey.SetValue(WATCHER_IP, value, RegistryValueKind.String); }
        }
        public string Port
        {
            get { return WatcherConfigKey.GetValue(WATCHER_PORT).ToString(); }
            set { WatcherConfigKey.SetValue(WATCHER_PORT, value, RegistryValueKind.String); }
        }
        public string PcName
        {
            get { return WatcherConfigKey.GetValue(WATCHER_PCNAME).ToString(); }
            set { WatcherConfigKey.SetValue(WATCHER_PCNAME, value, RegistryValueKind.String); }
        }
        public string Email
        {
            get { return WatcherConfigKey.GetValue(WATCHER_EMAIL).ToString(); }
            set { WatcherConfigKey.SetValue(WATCHER_EMAIL, value, RegistryValueKind.String); }
        }
        public string RootPath
        {
            get { return WatcherConfigKey.GetValue(ROOT_PATH).ToString(); }
            set { WatcherConfigKey.SetValue(ROOT_PATH, value, RegistryValueKind.String); }
        }
        public WatcherConfig(bool Write = false)
        {
            WatcherConfigKey = Registry.LocalMachine.OpenSubKey(MainData.WATCHER_CONFIG_PATH, Write);
            if (WatcherConfigKey == null)
            {
                WatcherConfigKey = Registry.LocalMachine.CreateSubKey(MainData.WATCHER_CONFIG_PATH);
                WatcherConfigKey.SetValue(WATCHER_CHECK_DELAY, DEFAULT_WATCHER_CHECK_DELAY, RegistryValueKind.DWord);
                WatcherConfigKey.SetValue(WATCHER_IP, "", RegistryValueKind.String);
                WatcherConfigKey.SetValue(WATCHER_PORT, "", RegistryValueKind.String);
                WatcherConfigKey.SetValue(WATCHER_PCNAME, "", RegistryValueKind.String);
                WatcherConfigKey.SetValue(WATCHER_EMAIL, "", RegistryValueKind.String);
                WatcherConfigKey.SetValue(ROOT_PATH, "", RegistryValueKind.String);
            }
        }
        public void Close() { if (WatcherConfigKey != null) WatcherConfigKey.Close(); }

        public void Delete()
        {
            WatcherConfigKey.DeleteSubKeyTree(MainData.WATCHER_CONFIG_PATH);
        }
    }
}
