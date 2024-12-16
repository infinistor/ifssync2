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
		const int FILTER_CHECK_DELAY = 5000;

		readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		readonly List<FilterThread> _filters = new List<FilterThread>();
		readonly JobDbManager _jobDb = new JobDbManager();
		readonly bool _global;

		public Filter(bool global)
		{
			_global = global;
		}

		public void CheckOnce()
		{
			FilterThreadAliveInit();
			var jobs = _jobDb.GetJobs(_global);

			foreach (var job in jobs)
			{
				if (job.JobName.Equals(MainData.INSTANT_BACKUP_NAME)) continue;
				bool isNewFilter = true;
				//if new or change
				foreach (var Filter in _filters)
				{
					if (Filter.IsAlive) continue;

					if (job.Id == Filter.Job.Id)
					{
						isNewFilter = false;
						Filter.IsAlive = true;
						//JobData is Changed.
						if (job.FilterUpdate)
						{
							job.FilterUpdate = false;
							Filter.JobDataUpdate(job);
							_jobDb.UpdateFilterCheck(job, _global);
						}
						break;
					}
				}

				//JobThread is not existed. Create JobThread
				if (isNewFilter) _filters.Add(new FilterThread(job));
			}

			for (int i = _filters.Count - 1; i >= 0; i--)
			{
				if (!_filters[i].IsAlive)
				{
					_filters[i].Close();
					_filters.RemoveAt(i);
					continue;
				}
				else _filters[i].FilterStateOn();

				if (!_filters[i].IsFilterUpdate) _filters[i].FilterUpdate();
			}

			Thread.Sleep(FILTER_CHECK_DELAY);
		}

		public void Stop()
		{
			FilterThreadAllDelete();
			_log.Info("Stop");
		}

		void FilterThreadAliveInit()
		{
			foreach (var Job in _filters) Job.IsAlive = false;
		}
		void FilterThreadAllDelete()
		{
			foreach (var Job in _filters) Job.Close();
			_filters.Clear();
		}

	}

}
