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
using log4net;
using log4net.Config;
using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using IfsSync2Data;

[assembly: XmlConfigurator(ConfigFile = "IfsSync2TrayIconLigConfig.xml", Watch = true)]

namespace IfsSync2TrayIcon
{
	class Program
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly TrayIconConfig TrayIconConfigs = new(true);

		static void Main()
		{
			Mutex mutex = new(true, MainData.MUTEX_NAME_TRAY_ICON, out bool CreateNew);

			if (!CreateNew)
			{
				log.Error("Prevent duplicate execution");
				return;
			}
			MainUtility.DeleteOldLogs(MainData.GetLogFolder("TrayIcon"));

			TrayIconManager TrayIcon = new();

			while (true)
			{
				try
				{
					if (TrayIcon.SetTray()) break;
					Delay(TrayIconConfigs.Delay);
				}
				catch (Exception e)
				{
					log.Error("SetTray Failed.", e);
				}
			}

			while (true)
			{
				try { TrayIcon.UpdateTray(); }
				catch (Exception e)
				{
					log.Error("UpdateTray Failed.", e);
					break;
				}
				Delay(TrayIconConfigs.Delay);
			}

			try { TrayIcon.Close(); }
			catch (Exception e)
			{
				log.Error("Close Failed.", e);
			}

		}

		private static void Delay(int MS)
		{

			DateTime ThisMoment = DateTime.Now;
			TimeSpan duration = new(0, 0, 0, 0, MS);
			DateTime AfterWards = ThisMoment.Add(duration);
			while (AfterWards >= ThisMoment)
			{
				Application.DoEvents();
				ThisMoment = DateTime.Now;
				Thread.Sleep(100);
			}
		}

	}
}
