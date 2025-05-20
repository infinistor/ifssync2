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
using log4net;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using IfsSync2Common;
using System.Linq;

namespace IfsSync2Filter
{
	public class Filter()
	{
		readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		readonly List<FilterThread> _filters = [];
		readonly JobDbManager _jobDb = new();

		public void CheckOnce()
		{
			// DB에서 현재 jobs 가져오기
			var jobs = _jobDb.GetJobs();
			var activeJobIds = jobs.Where(j => !j.JobName.Equals(IfsSync2Constants.INSTANT_BACKUP_NAME))
									.Select(j => j.Id)
									.ToHashSet();

			// 제거해야 할 필터 찾기
			var filtersToRemove = _filters.Where(f => !activeJobIds.Contains(f.Job.Id)).ToList();

			// 제거할 필터는 Close 후 제거
			foreach (var filter in filtersToRemove)
			{
				filter.Close();
				_filters.Remove(filter);
			}

			// 존재하는 필터 업데이트 및 없는 필터 추가
			foreach (var job in jobs)
			{
				if (job.JobName.Equals(IfsSync2Constants.INSTANT_BACKUP_NAME))
					continue;

				var existingFilter = _filters.FirstOrDefault(f => f.Job.Id == job.Id);

				if (existingFilter != null)
				{
					// 기존 필터 업데이트 필요시
					if (job.FilterUpdate)
					{
						job.FilterUpdate = false;
						existingFilter.JobDataUpdate(job);
						_jobDb.UpdateFilterCheck(job);
					}

					// 필터 상태 갱신
					existingFilter.FilterStateOn();

					// 필터 업데이트 필요하면 수행
					if (!existingFilter.IsFilterUpdate)
						existingFilter.FilterUpdate();
				}
				else
				{
					// 새 필터 생성
					var newFilter = new FilterThread(job);
					_filters.Add(newFilter);
				}
			}

			Thread.Sleep(IfsSync2Constants.DEFAULT_FILTER_CHECK_DELAY);
		}

		public void Stop()
		{
			FilterThreadAllDelete();
			_log.Info("Stop");
		}

		void FilterThreadAllDelete()
		{
			foreach (var Job in _filters) Job.Close();
			_filters.Clear();
		}

	}

}
