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
using IfsSync2Data;
using log4net;
using Microsoft.Win32;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace IfsSync2TrayIcon
{
    class TrayIconManager
    {
        private static readonly string CLASS_NAME = "TrayIconManager";

        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private NotifyIcon Tray;
        private readonly TrayIconConfig TrayIconConfigs = new TrayIconConfig(true);
        private readonly SenderConfig SenderConfigs = new SenderConfig(true);

        private string DetailMessage = string.Empty;
        private string SummaryMessage = string.Empty;
        private const int DefaultBalloonTipDelay = 5 * 1000; //5sec
        public TrayIconManager()
        {
            Clear();
        }

        public bool SetTray()
        {
            const string FUNCTION_NAME = "SetTray";

            //출처: https://overimagine.tistory.com/89 [Over Imagine]
            //Set tray option
            try
            {
                Tray = new NotifyIcon { Visible = true, Icon = new System.Drawing.Icon(TrayIconConfigs.IconPath) };
                Tray.Click += delegate (object click, EventArgs e) { IconClickEvent(); };
                
                ContextMenu Menu = new ContextMenu();
                MenuItem SenderStop = new MenuItem
                {
                    Index = 0,
                    Text = "일시중지",
                };
                SenderStop.Click += SenderStop_Click;
                if (SenderConfigs.Stop) SenderStop.Text = "재시작";

                Menu.MenuItems.Add(SenderStop);
                Tray.ContextMenu = Menu;

                log.InfoFormat("[{0}:{1}] Create Tray Icon", CLASS_NAME, FUNCTION_NAME);

                return true;
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] Tray registration failure: {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
                return false;
            }
        }

        private void SenderStop_Click(object sender, EventArgs e)
        {
            MenuItem menu = sender as MenuItem;
            if (SenderConfigs.Stop)
            {
                SenderConfigs.Stop = false;
                menu.Text = "일시중지";
            }
            else
            {
                SenderConfigs.Stop = true;
                menu.Text = "재시작";
            }
        }
        private void IconClickEvent()
        {
            Console.WriteLine("Test");
            try
            {
                Tray.ShowBalloonTip(DefaultBalloonTipDelay, "IfsSync2", SummaryMessage, ToolTipIcon.Info);
            }
            catch(Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] ShowBalloonTip failure: {3}", CLASS_NAME, "IconClickEvent", "Exception", e.Message);
            }
        }

        public void UpdateTray()
        {
            const string FUNCTION_NAME = "UpdateTray";

            long MainRemaining = 0;
            long MainRemainingSize = 0;
            long MainUploadCount = 0;
            long MainUploadFailCount = 0;
            long MainUploadSize = 0;

            DetailMessage = string.Empty;
            SummaryMessage = string.Empty;
            try
            {
                RegistryKey UserKey = Registry.LocalMachine.OpenSubKey(MainData.REGISTRY_ROOT + "Job");
                string[] UserKeyList = UserKey.GetSubKeyNames();

                foreach (string UserName in UserKeyList)
                {
                    RegistryKey JobKey = Registry.LocalMachine.OpenSubKey(MainData.REGISTRY_ROOT + MainData.JOB_CONFIG_NAME + UserName);

                    string[]JobKeyList = JobKey.GetSubKeyNames();

                    foreach(string JobName in JobKeyList)
                    {
                        JobState State = new JobState(UserName, JobName);

                        long Remaining       = State.RemainingCount;
                        long RemainingSize   = State.RemainingSize;
                        long UploadCount     = State.UploadCount;
                        long UploadFailCount = State.UploadFailCount;
                        long UploadSize      = State.UploadSize;

                        DetailMessage += string.Format("{0} : {1}\nRemaining File : {2} ({3})\nUpload File : {4} ({5})\n",
                                        State.JobName, State.Status, Remaining, MainData.SizeToString(RemainingSize), UploadCount, MainData.SizeToString(UploadSize));

                        MainRemaining       += Remaining;
                        MainRemainingSize   += RemainingSize;
                        MainUploadCount     += UploadCount;
                        MainUploadFailCount += UploadFailCount;
                        MainUploadSize      += UploadSize;
                    }
                }
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] Registry Key Read Faill: {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
            }

            TrayIconConfigs.Remaining       = MainRemaining;
            TrayIconConfigs.RemainingSize   = MainRemainingSize;
            TrayIconConfigs.UploadCount     = MainUploadCount;
            TrayIconConfigs.UploadFailCount = MainUploadFailCount;
            TrayIconConfigs.FileSize        = MainUploadSize;

            //Tray.Text =
            SummaryMessage = 
                string.Format("Remaining Files     : {0}({1})\n" +
                              "Uploaded Files      : {2}({3})\n" +
                              "Upload Failed Files : {4}\n" ,
                MainRemaining, MainData.SizeToString(MainRemainingSize),
                MainUploadCount, MainData.SizeToString(MainUploadSize),
                MainUploadFailCount);
        }

        private void Clear()
        {
            const string FUNCTION_NAME = "Clear";

            try
            {
                RegistryKey UserKey = Registry.LocalMachine.OpenSubKey(MainData.REGISTRY_ROOT + "Job");
                string[] UserKeyList = UserKey.GetSubKeyNames();

                foreach (string UserName in UserKeyList)
                {
                    RegistryKey JobKey = Registry.LocalMachine.OpenSubKey(MainData.REGISTRY_ROOT + MainData.JOB_CONFIG_NAME + UserName);

                    string[] JobKeyList = JobKey.GetSubKeyNames();

                    foreach (string JobName in JobKeyList) new JobState(UserName, JobName, true).UploadClear();
                }
            }
            catch (Exception e)
            {
                log.ErrorFormat("[{0}:{1}:{2}] Registry Key Read Faill: {3}", CLASS_NAME, FUNCTION_NAME, "Exception", e.Message);
            }
        }

        public void Close()
        {
            if (Tray != null) Tray.Dispose();
        }
    }
}
