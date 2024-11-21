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
using log4net;
using log4net.Config;
using System.Reflection;
using IfsSync2Data;
using System.Collections.Generic;
using System;

[assembly: XmlConfigurator(ConfigFile = "IfsSync2WatcherServiceLogConfig.xml", Watch = true)]

namespace IfsSync2WatcherService
{
	class Watcher
	{
		private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly WatcherConfig WatcherConfigs;
		private readonly FilterConfig FilterConfigs;
		private readonly SenderConfig SenderConfigs;
		private readonly TrayIconConfig TrayIconConfigs;

		private readonly JobDataSqlManager JobSQL;
		private readonly UserDataSqlManager UserSQL;

		public Watcher()
		{
			WatcherConfigs = new WatcherConfig(true);
			FilterConfigs = new FilterConfig(true);
			SenderConfigs = new SenderConfig(true);
			TrayIconConfigs = new TrayIconConfig();

			JobSQL = new JobDataSqlManager();
			UserSQL = new UserDataSqlManager();
		}

		public void CheckOnce()
		{
			if (WatcherConfigs.IP != "")
			{
				string URL = MainData.CreateAddress(WatcherConfigs.IP, WatcherConfigs.Port);
				UserData GlobalUser;
				try
				{
					List<UserData> GlobalUserList = UserSQL.GetUsers(true);
					if (GlobalUserList.Count == 0)
					{
						log.Error("Global User is empty!");
						return;
					}

					GlobalUser = GlobalUserList[0];
				}
				catch (Exception e)
				{
					log.Error("Global User Error", e);
					return;
				}


				try
				{
					IFSSyncUtility.CheckUpdate(URL, GlobalUser.UserName, MainData.GetVersion());
				}
				catch (Exception e)
				{
					log.Error("CheckUpdate Error", e);
				}

				//글로벌 잡 가져오기
				List<JobData> GlobalJobList = JobSQL.GetJobs(true);

				try
				{
					//글로벌 옵션 가져오기
					var GlobalConfig = IFSSyncUtility.GetGlobalConfig(URL, GlobalUser.UserName, GlobalUser.Id);

					//s3proxy 정보 확인
					if (!string.IsNullOrWhiteSpace(GlobalConfig.S3Proxy))
					{
						if (GlobalUser.URL != GlobalConfig.S3Proxy) UserSQL.UpdateUserS3Proxy(GlobalUser.Id, GlobalConfig.S3Proxy, true);
					}

					//글로벌 옵션 설정
					SenderConfigs.Stop = GlobalConfig.SenderPause;
					SenderConfigs.SenderDelay = GlobalConfig.SenderDelay;
					SenderConfigs.FetchCount = GlobalConfig.FetchCount;

					//새로운 글로벌 잡 가져오기
					List<JobData> NewGlobalJobList = GlobalConfig.JobList;

					//Delete Flag on
					foreach (var MainJob in GlobalJobList) MainJob.DeleteFlag = true;

					foreach (var NewJob in NewGlobalJobList)
					{
						bool CreateCheck = true;

						foreach (var MainJob in GlobalJobList)
						{
							if (!MainJob.DeleteFlag) continue;
							if (NewJob.StrPath == MainJob.StrPath)
							{
								CreateCheck = false;
								MainJob.DeleteFlag = false;
								NewJob.Id = MainJob.Id;
								//기존과 비교하여 변경점이 있으면 변경
								if (!NewJob.Equals(MainJob)) JobSQL.Update(NewJob, true);
								break;
							}
						}

						if (CreateCheck)
						{
							int index = JobSQL.NextGlobalJobIndex();
							NewJob.JobName = MainData.DEFAULT_GLOBAL_JOB_NAME + index.ToString();
							JobSQL.Insert(NewJob, true);
						}
					}
					foreach (var MainJob in GlobalJobList)
					{
						if (MainJob.DeleteFlag) JobSQL.Delete(MainJob.Id, true);
					}

				}
				catch (Exception e)
				{
					log.Error("Global Job Get Error", e);
					return;
				}

				//Alive 신호보내기
				AliveData Alive = new AliveData()
				{
					ListenAlive = FilterConfigs.Alive,
					SenderAlive = SenderConfigs.Alive,
					MonRemain = TrayIconConfigs.Remaining,
					FailRemain = TrayIconConfigs.UploadFailCount
				};

				if (!IFSSyncUtility.SendAlive(URL, GlobalUser.UserName, Alive, out string Error))
					log.Error($"Alive send fail {Error}");
				else
					log.Info("Alive send OK");

				//Alive init
				FilterConfigs.Alive = false;
				SenderConfigs.Alive = false;

			}
		}

		public void Stop()
		{

		}
	}
}
