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
using log4net;
using log4net.Config;
using System.Reflection;
using IfsSync2Common;
using System.Threading;
using System;

[assembly: XmlConfigurator(ConfigFile = "IfsSync2SenderLogConfig.xml", Watch = true)]

namespace IfsSync2Sender
{
	static class Program
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		static void Main()
		{
			var mutex = new Mutex(true, IfsSync2Constants.MUTEX_NAME_SENDER, out bool CreateNew);
			if (!CreateNew)
			{
				log.Error("Prevent duplicate execution");
				return;
			}
			log.Error("Main Start");
			MainUtility.DeleteOldLogs(IfsSync2Utilities.GetLogFolder("Sender"));

			var senderConfigs = new SenderConfig(true);
			var senderManager = new SenderManager();

			try
			{
				while (true)
				{
					try
					{
						while (senderConfigs.Stop)
						{
							senderManager.AllStop();
							Thread.Sleep(senderConfigs.SenderCheckDelay);
						}

						senderConfigs.Alive = true;
						senderManager.Once(senderConfigs.FetchCount, senderConfigs.SenderDelay, senderConfigs.RetryDelay, senderConfigs.ThreadCount, senderConfigs.MultipartUploadFileSize, senderConfigs.MultipartUploadPartSize, senderConfigs.LogRetention);

						Thread.Sleep(senderConfigs.SenderCheckDelay);
					}
					catch (Exception e)
					{
						log.Error("Sender Error", e);
						mutex.ReleaseMutex();
						Thread.Sleep(senderConfigs.SenderCheckDelay);
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
