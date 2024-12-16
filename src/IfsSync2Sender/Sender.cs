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
using System;
using System.Collections.Generic;
using System.Reflection;
using IfsSync2Data;
using log4net;

namespace IfsSync2Sender
{
	class Sender
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		//SQL
		private readonly string RootPath;
		private readonly JobDbManager JobSQL;
		private readonly UserDbManager UserSQL;
		private readonly List<SenderThread> SenderList = new List<SenderThread>();
		//User Data
		private List<UserData> UserList;
		private List<UserData> GlobalUserList;

		private bool Global { get; set; }

		public Sender(string _RootPath, bool _Global)
		{
			Global = _Global;
			RootPath = _RootPath;
			JobSQL = new JobDbManager();
			UserSQL = new UserDbManager();
		}

		public void Once(int FetchCount, int SenderDelay)
		{
			UserDataUpdate();
			SenderThreadInit(FetchCount, SenderDelay);
			SenderQuitCheck();

			//Job Data
			List<JobData> JobList = JobSQL.GetJobs(Global);

			foreach (JobData Job in JobList)
			{
				try
				{
					bool IsNewSender = true;

					UserData User = GetUserData(UserList, GlobalUserList, Job.IsGlobalUser, Job.UserID);

					if (User.Id == 0) continue;//User 데이터가 존재하지 않음

					foreach (SenderThread Sender in SenderList)
					{
						if (Sender.IsAlive) continue;
						if (Job.Id == Sender.Job.Id)
						{
							//UserData Changed
							if (User.UpdateFlag) Sender.End();
							//JobData Changed
							else if (Job.SenderUpdate)
							{
								JobSQL.UpdateSenderCheck(Job, Global);

								if (!Sender.Stop) Sender.Stop = true;
								Sender.Job.CopyTo(Job);
								Sender.Stop = false;

								IsNewSender = false;
								Sender.IsAlive = true;
							}
							else
							{

								IsNewSender = false;
								Sender.IsAlive = true;
								Sender.Job.IsInit = Job.IsInit;
							}
						}
					}

					//실행 시간이 아님 
					if (!Job.CheckToSchedules()) continue;

					//Sender Create
					if (IsNewSender) SenderList.Add(new SenderThread(Job, User, FetchCount, SenderDelay, Global));
					if (Job.SenderUpdate) JobSQL.UpdateSenderCheck(Job, Global);

				}
				catch (Exception e)
				{
					log.Error("JobName({Job.JobName})", e);
				}
			}

			for (int i = SenderList.Count - 1; i >= 0; i--)
			{
				//Alive Check
				if (!SenderList[i].IsAlive) SenderList[i].End();
			}
			SenderQuitCheck();
		}

		public void AllStop()
		{
			for (int i = SenderList.Count - 1; i >= 0; i--)
				SenderList[i].End();
		}

		private void UserDataUpdate()
		{
			GlobalUserList = UserSQL.GetUsers(true);
			UserList = UserSQL.GetUsers(false);

			foreach (var User in GlobalUserList) if (User.UpdateFlag) UserSQL.UpdateUserCheck(User, true);
			foreach (var User in UserList) if (User.UpdateFlag) UserSQL.UpdateUserCheck(User, false);
		}

		private void SenderThreadInit(int FetchCount, int SenderDelay)
		{
			foreach (var Item in SenderList)
			{
				Item.Delay = SenderDelay;
				Item.FetchCount = FetchCount;
				Item.IsAlive = false;
			}
		}
		private void SenderQuitCheck()
		{
			for (int i = SenderList.Count - 1; i >= 0; i--)
			{
				//End Check
				if (SenderList[i].Quit)
				{
					SenderList[i].Close();
					log.Debug($"JobName({SenderList[i].Job.JobName}) Delete");
					SenderList.RemoveAt(i);
				}
			}
		}
		static UserData GetUserData(List<UserData> UserList, List<UserData> GlobalUserList, bool IsGlobalUser, int UserID)
		{
			if (IsGlobalUser)
			{
				foreach (UserData User in GlobalUserList) if (User.Id == UserID) return User;
			}
			else
			{
				foreach (UserData User in UserList) if (User.Id == UserID) return User;
			}

			return new UserData();
		}
	}
}
