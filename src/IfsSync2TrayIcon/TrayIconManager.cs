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
using IfsSync2Data;
using log4net;
using Microsoft.Win32;
using System;
using System.Reflection;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace IfsSync2TrayIcon
{
	[SupportedOSPlatform("windows")]
	class TrayIconManager
	{
		private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private NotifyIcon Tray;
		private readonly TrayIconConfig TrayIconConfigs = new(true);
		private readonly SenderConfig SenderConfigs = new(true);

		private string DetailMessage;
		private string SummaryMessage = string.Empty;
		private const int DefaultBalloonTipDelay = 5 * 1000; //5sec
		public TrayIconManager()
		{
			Clear();
		}

		public bool SetTray()
		{
			//출처: https://overimagine.tistory.com/89 [Over Imagine]
			//Set tray option
			try
			{
				Tray = new NotifyIcon { Visible = true, Icon = new System.Drawing.Icon(TrayIconConfigs.IconPath) };
				Tray.Click += delegate (object click, EventArgs e) { IconClickEvent(); };

				var Menu = new ContextMenuStrip();
				var SenderStop = new ToolStripMenuItem { Text = "일시중지", Name = "SenderStop" };
				SenderStop.Click += SenderStop_Click;
				if (SenderConfigs.Stop) SenderStop.Text = "재시작";
				Menu.Items.Add(SenderStop);
				Tray.ContextMenuStrip = Menu;

				log.Info("Create Tray Icon");

				return true;
			}
			catch (Exception e)
			{
				log.Error("Tray registration failure.", e);
				return false;
			}
		}

		private void SenderStop_Click(object sender, EventArgs e)
		{
			var menu = sender as ContextMenuStrip;
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
			catch (Exception e)
			{
				log.Error("ShowBalloonTip failure.", e);
			}
		}

		public void UpdateTray()
		{
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

					string[] JobKeyList = JobKey.GetSubKeyNames();

					foreach (string JobName in JobKeyList)
					{
						var State = new JobStatus(UserName, JobName);

						long Remaining = State.RemainingCount;
						long RemainingSize = State.RemainingSize;
						long UploadCount = State.UploadCount;
						long UploadFailCount = State.UploadFailCount;
						long UploadSize = State.UploadSize;

						DetailMessage += $"{State.JobName} : {State.Status}\nRemaining File : {Remaining} ({MainData.SizeToString(RemainingSize)})\nUpload File : {UploadCount} ({MainData.SizeToString(UploadSize)})\n";

						MainRemaining += Remaining;
						MainRemainingSize += RemainingSize;
						MainUploadCount += UploadCount;
						MainUploadFailCount += UploadFailCount;
						MainUploadSize += UploadSize;
					}
				}
			}
			catch (Exception e)
			{
				log.Error("Registry Key Read Faill", e);
			}

			TrayIconConfigs.Remaining = MainRemaining;
			TrayIconConfigs.RemainingSize = MainRemainingSize;
			TrayIconConfigs.UploadCount = MainUploadCount;
			TrayIconConfigs.UploadFailCount = MainUploadFailCount;
			TrayIconConfigs.FileSize = MainUploadSize;

			//Tray.Text =
			SummaryMessage = $"Remaining Files     : {MainRemaining}({MainData.SizeToString(MainRemainingSize)})\n" +
							 $"Uploaded Files      : {MainUploadCount}({MainData.SizeToString(MainUploadSize)})\n" +
							 $"Upload Failed Files : {MainUploadFailCount}\n";
		}

		private void Clear()
		{
			try
			{
				RegistryKey UserKey = Registry.LocalMachine.OpenSubKey(MainData.REGISTRY_ROOT + "Job");
				string[] UserKeyList = UserKey.GetSubKeyNames();

				foreach (string UserName in UserKeyList)
				{
					RegistryKey JobKey = Registry.LocalMachine.OpenSubKey(MainData.REGISTRY_ROOT + MainData.JOB_CONFIG_NAME + UserName);

					string[] JobKeyList = JobKey.GetSubKeyNames();

					foreach (string JobName in JobKeyList) new JobStatus(UserName, JobName, true).UploadClear();
				}
			}
			catch (Exception e)
			{
				log.Error("Registry Key Read Faill.", e);
			}
		}

		public void Close()
		{
			Tray?.Dispose();
		}
	}
}
