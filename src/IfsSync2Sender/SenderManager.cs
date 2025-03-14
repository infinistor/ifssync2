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
using System.Collections.Generic;
using System.Reflection;
using IfsSync2Data;
using log4net;
using System.Linq;

namespace IfsSync2Sender
{
	class SenderManager()
	{
		static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		//SQL
		readonly JobDbManager _jobManager = new();
		readonly UserDbManager _userManager = new();
		readonly List<Sender> _senders = [];
		InstantSender _instantSender;

		//User Data
		List<UserData> _users;
		List<UserData> _globalUsers;

		public void Once(int fetchCount, int senderDelay, int threadCount, long multipartUploadFileSize, long multipartUploadPartSize)
		{
			UserDataUpdate();
			SenderQuitCheck();

			//Job Data
			var jobs = _jobManager.GetJobs();

			// instent 작업만 별도로 처리.단 한개만 존재함.
			var instantJob = jobs.FirstOrDefault(job => job.JobName == MainData.INSTANT_BACKUP_NAME);

			// 인스턴트 백업 작업이 존재하고 작업을 시작할 경우 처리
			if (instantJob != null)
			{
				if (instantJob.FilterUpdate)
				{
					// 인스턴트 백업 작업 사용자 데이터 확인
					var user = GetUserData(_users, _globalUsers, instantJob.IsGlobalUser, instantJob.UserId);

					// 인스턴트 백업 Sender가 없으면 생성
					if (_instantSender == null)
					{
						_instantSender = new(instantJob, user, fetchCount, senderDelay, threadCount, multipartUploadFileSize, multipartUploadPartSize);
						_instantSender.Run();
						log.Debug($"인스턴트 백업 Sender 생성: {instantJob.JobName}");
					}
					// 사용자 데이터가 변경되었을 경우 처리
					if (user.UpdateFlag) _instantSender.UpdateUser(user);

					// 작업 파라미터 업데이트
					_instantSender.Update(fetchCount, senderDelay, threadCount, multipartUploadFileSize, multipartUploadPartSize);
				}

				// 인스턴트 작업은 일반 작업 목록에서 제외
				jobs = jobs.Where(job => job.JobName != MainData.INSTANT_BACKUP_NAME).ToList();
			}
			else if (_instantSender != null)
			{
				// 인스턴트 작업이 없어졌으면 Sender 종료
				_instantSender.Close();
				_instantSender = null;
				_jobManager.UpdateFilterCheck(instantJob);
				log.Debug("인스턴트 백업 Sender 종료");
			}
			else
			{
				_jobManager.UpdateFilterCheck(instantJob);
			}

			foreach (var job in jobs)
			{
				// 현재 작업의 사용자 데이터 확인
				var user = GetUserData(_users, _globalUsers, job.IsGlobalUser, job.UserId);

				// 해당 sender 존재 여부 확인
				var sender = _senders.FirstOrDefault(sender => sender.Job.Id == job.Id);

				// 해당 sender 존재
				if (sender != null)
				{
					// 사용자 데이터가 변경되었을 경우 sender 스레드 업데이트
					if (user.UpdateFlag) sender.UpdateUser(user);
					// 작업 파라미터 업데이트

					sender.Update(fetchCount, senderDelay, threadCount, multipartUploadFileSize, multipartUploadPartSize);
				}
				// 해당 sender 존재하지 않음
				else
				{
					// sender 생성
					sender = new(job, user, fetchCount, senderDelay, threadCount, multipartUploadFileSize, multipartUploadPartSize);
					sender.Run();
					_senders.Add(sender);
				}
			}
		}

		public void AllStop()
		{
			_senders.ForEach(sender => sender.Close());

			// 인스턴트 백업 Sender도 종료
			if (_instantSender != null)
			{
				_instantSender.Close();
				_instantSender = null;
			}
		}

		private void UserDataUpdate()
		{
			_globalUsers = _userManager.GetUsers(true);
			_users = _userManager.GetUsers(false);

			_globalUsers.Where(user => user.UpdateFlag).ToList().ForEach(user => _userManager.UpdateUserCheck(user, true));
			_users.Where(user => user.UpdateFlag).ToList().ForEach(user => _userManager.UpdateUserCheck(user, false));
		}


		private void SenderQuitCheck()
		{
			// 인스턴트 백업 Sender 종료 확인
			if (_instantSender != null && _instantSender.Quit)
			{
				_instantSender.Close();
				_instantSender = null;
				log.Debug("인스턴트 백업 Sender 종료됨");
			}

			_senders.RemoveAll(sender =>
			{
				if (sender.Quit)
				{
					sender.Close();
					log.Debug($"JobName({sender.Job.JobName}) Delete");
					return true;
				}
				return false;
			});
		}

		static UserData GetUserData(List<UserData> users, List<UserData> globalUsers, bool isGlobalUser, int userId)
			=> (isGlobalUser ? globalUsers : users).FirstOrDefault(user => user.Id == userId) ?? new UserData();
	}
}
