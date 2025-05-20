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
using IfsSync2Common;
using log4net;
using Microsoft.Win32;
using System;
using System.Windows.Forms;
using System.Runtime.Versioning;

namespace IfsSync2TrayIcon
{
	[SupportedOSPlatform("windows")]
	class TrayIconManager
	{
		private readonly ILog log = LogManager.GetLogger(typeof(TrayIconManager));
		private NotifyIcon tray;
		private readonly TrayIconConfig trayIconConfigs = new(true);
		private readonly SenderConfig senderConfigs = new(true);

		private string detailMessage;
		private string summaryMessage = string.Empty;
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
				tray = new NotifyIcon { Visible = true, Icon = new System.Drawing.Icon(trayIconConfigs.IconPath) };
				tray.Click += delegate (object click, EventArgs e) { IconClickEvent(); };

				var menu = new ContextMenuStrip();
				var senderStop = new ToolStripMenuItem { Text = "일시중지", Name = "SenderStop" };
				senderStop.Click += SenderStop_Click;
				if (senderConfigs.Stop) senderStop.Text = "재시작";
				menu.Items.Add(senderStop);
				tray.ContextMenuStrip = menu;

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
			if (senderConfigs.Stop)
			{
				senderConfigs.Stop = false;
				menu.Text = "일시중지";
			}
			else
			{
				senderConfigs.Stop = true;
				menu.Text = "재시작";
			}
		}
		private void IconClickEvent()
		{
			Console.WriteLine("Test");
			try
			{
				tray.ShowBalloonTip(DefaultBalloonTipDelay, "IfsSync2", summaryMessage, ToolTipIcon.Info);
			}
			catch (Exception e)
			{
				log.Error("ShowBalloonTip failure.", e);
			}
		}

		public void UpdateTray()
		{
			long mainRemaining = 0;
			long mainRemainingSize = 0;
			long mainUploadCount = 0;
			long mainUploadFailCount = 0;
			long mainUploadSize = 0;

			detailMessage = string.Empty;
			summaryMessage = string.Empty;
			try
			{
				RegistryKey userKey = Registry.LocalMachine.OpenSubKey(IfsSync2Constants.REGISTRY_ROOT + "Job");
				string[] userKeyList = userKey.GetSubKeyNames();

				foreach (string userName in userKeyList)
				{
					RegistryKey jobKey = Registry.LocalMachine.OpenSubKey(IfsSync2Constants.REGISTRY_ROOT + IfsSync2Constants.JOB_CONFIG_NAME + userName);

					string[] jobKeyList = jobKey.GetSubKeyNames();

					foreach (string jobName in jobKeyList)
					{
						var state = new JobStatus(userName, jobName);

						long remaining = state.RemainingCount;
						long remainingSize = state.RemainingSize;
						long uploadCount = state.UploadCount;
						long uploadFailCount = state.UploadFailCount;
						long uploadSize = state.UploadSize;

						detailMessage += $"{state.JobName} : {state.Status}\nRemaining File : {remaining} ({CapacityUnit.Format(remainingSize)})\nUpload File : {uploadCount} ({CapacityUnit.Format(uploadSize)})\n";

						mainRemaining += remaining;
						mainRemainingSize += remainingSize;
						mainUploadCount += uploadCount;
						mainUploadFailCount += uploadFailCount;
						mainUploadSize += uploadSize;
					}
				}
			}
			catch (Exception e)
			{
				log.Error("Registry Key Read Faill", e);
			}

			trayIconConfigs.Remaining = mainRemaining;
			trayIconConfigs.RemainingSize = mainRemainingSize;
			trayIconConfigs.UploadCount = mainUploadCount;
			trayIconConfigs.UploadFailCount = mainUploadFailCount;
			trayIconConfigs.FileSize = mainUploadSize;

			//tray.Text =
			summaryMessage = $"Remaining Files     : {mainRemaining}({CapacityUnit.Format(mainRemainingSize)})\n" +
							 $"Uploaded Files      : {mainUploadCount}({CapacityUnit.Format(mainUploadSize)})\n" +
							 $"Upload Failed Files : {mainUploadFailCount}\n";
		}

		private void Clear()
		{
			try
			{
				RegistryKey userKey = Registry.LocalMachine.OpenSubKey(IfsSync2Constants.REGISTRY_ROOT + "Job");
				string[] userKeyList = userKey.GetSubKeyNames();

				foreach (string userName in userKeyList)
				{
					RegistryKey jobKey = Registry.LocalMachine.OpenSubKey(IfsSync2Constants.REGISTRY_ROOT + IfsSync2Constants.JOB_CONFIG_NAME + userName);

					string[] jobKeyList = jobKey.GetSubKeyNames();

					foreach (string jobName in jobKeyList) new JobStatus(userName, jobName, true).UploadClear();
				}
			}
			catch (Exception e)
			{
				log.Error("Registry Key Read Faill.", e);
			}
		}

		public void Close()
		{
			tray?.Dispose();
		}
	}
}
