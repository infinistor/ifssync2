﻿/*
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
using log4net.Config;
using System;
using System.Threading;

[assembly: XmlConfigurator(ConfigFile = "IfsSync2FilterLogConfig.xml", Watch = true)]

namespace IfsSync2Filter
{
	static class Program
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Program));

		static void Main()
		{
			var mutex = new Mutex(true, IfsSync2Constants.MUTEX_NAME_FILTER, out bool CreateNew);
			if (!CreateNew)
			{
				log.Error("Prevent duplicate execution");
				return;
			}
			log.Error("Main Start");

			MainUtility.DeleteOldLogs(IfsSync2Utilities.GetLogFolder("Filter"));

			var filterConfigs = new FilterConfig(true);

			var filter = new Filter();

			try
			{
				while (true)
				{
					filterConfigs.Alive = true;
					try
					{
						filter.CheckOnce();
						log.Info("Filter Check End");
						Thread.Sleep(filterConfigs.FilterCheckDelay);
					}
					catch (Exception e)
					{
						log.Error("Filter Check Error", e);
						Thread.Sleep(filterConfigs.FilterCheckDelay);
						break;
					}
				}
			}
			finally
			{
				mutex.ReleaseMutex();
			}
		}
	}
}
