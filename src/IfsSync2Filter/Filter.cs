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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using IfsSync2Data;

namespace IfsSync2Filter
{
	public class Filter
	{
		static readonly int FILTER_CHECK_DELAY = 5000;
		static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		readonly List<FilterThread> FilterList = [];
		readonly JobDbManager JobSQL;
		readonly bool Global;

		public Filter(bool global)
		{
			Global = global;
			JobSQL = new JobDbManager();
		}

		public void CheckOnce()
		{
			FilterThreadAliveInit();
			List<JobData> TempJobList = JobSQL.GetJobs(Global);

			foreach (JobData Item in TempJobList)
			{
				if (Item.JobName.Equals(MainData.INSTANT_BACKUP_NAME)) continue;
				bool IsNewFilter = true;
				//if new or change
				foreach (FilterThread Filter in FilterList)
				{
					if (Filter.IsAlive) continue;

					if (Item.Id == Filter.Job.Id)
					{
						IsNewFilter = false;
						Filter.IsAlive = true;
						//JobData is Changed.
						if (Item.FilterUpdate)
						{
							Item.FilterUpdate = false;
							Filter.JobDataUpdate(Item);
							JobSQL.UpdateFilterCheck(Item, Global);
						}
						break;
					}
				}

				//JobThread is not existed. Create JobThread
				if (IsNewFilter) FilterList.Add(new FilterThread(Item));
			}

			for (int i = FilterList.Count - 1; i >= 0; i--)
			{
				if (!FilterList[i].IsAlive)
				{
					FilterList[i].Close();
					FilterList.RemoveAt(i);
					continue;
				}
				else FilterList[i].FilterStateOn();

				if (!FilterList[i].IsFilterUpdate) FilterList[i].FilterUpdate();
			}

			Thread.Sleep(FILTER_CHECK_DELAY);
		}

		public void Stop()
		{
			FilterThreadAllDelete();
			log.Info("Stop");
		}

		void FilterThreadAliveInit()
		{
			foreach (FilterThread Job in FilterList) Job.IsAlive = false;
		}
		void FilterThreadAllDelete()
		{
			foreach (FilterThread Job in FilterList) Job.Close();
			FilterList.Clear();
		}

	}

}
