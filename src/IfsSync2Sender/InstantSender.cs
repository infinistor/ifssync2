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
using System;
using System.Reflection;
using IfsSync2Data;
using System.Collections.Generic;

namespace IfsSync2Sender
{
	/// <summary>
	/// 인스턴트 백업 전용 Sender 클래스
	/// </summary>
	/// <remarks>
	/// 인스턴트 백업 전용 Sender 생성자
	/// </remarks>
	public class InstantSender(JobData jobData, UserData userData, int fetchCount, int delayTime, int threadCount, long multipartUploadFileSize, long multipartUploadPartSize) : Sender(jobData, userData, fetchCount, delayTime, threadCount, multipartUploadFileSize, multipartUploadPartSize)
	{
		const string INSTANT_BACKUP_START = "Instant Backup Start";
		const string INSTANT_BACKUP_ANALYSIS = "Analysis File count = ";
		const string INSTANT_BACKUP_ZERO = "Analysis Zero.";
		const string INSTANT_BACKUP_STOP = "Backup Stop!";
		const string INSTANT_BACKUP_FINISH = "Backup Finish!";

		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly InstantData _instant = new();

		/// <summary>
		/// 인스턴트 백업 시작
		/// </summary>
		public override void Start()
		{
			// 상태 체크
			if (_status.Quit)
			{
				_jobManager.UpdateAllCheck(Job);
				return;
			}

			_log.Debug("InstantSender Start");

			if (!Login())
			{
				End();
				return;
			}

			// 인스턴트 백업 실행
			ExecuteInstantBackup();

			End();
		}

		/// <summary>
		/// 인스턴트 백업 실행
		/// </summary>
		private void ExecuteInstantBackup()
		{
			_log.Debug(INSTANT_BACKUP_START);
			_taskManager.InsertLog(INSTANT_BACKUP_START);

			// 분석 작업이 완료되지 않았을 경우 처리
			if (!_instant.Analysis)
			{
				List<ShadowCopy> shadowCopyList = _shadowCopyManager.CreateShadowCopies(Job.Path);
				if (shadowCopyList.Count > 0)
				{
					_status.VSS = true;
					_taskManager.InsertLog($"VSS Created: {shadowCopyList.Count} shadow copies");
				}

				_instant.Clear();
				_status.UploadClear();

				_instant.Total = Analysis();
				_taskManager.InsertLog($"{INSTANT_BACKUP_ANALYSIS}{_instant.Total} files");
				_instant.Analysis = true;

				if (_status.Quit)
				{
					_taskManager.Clear();
					_log.Debug(INSTANT_BACKUP_STOP);
					_taskManager.InsertLog(INSTANT_BACKUP_STOP);
				}
				else if (_instant.Total != 0)
				{
					_taskManager.InsertLog("Starting backup process...");
					InstantRunOnce(shadowCopyList);
				}
				else
				{
					_log.Debug(INSTANT_BACKUP_ZERO);
					_taskManager.InsertLog(INSTANT_BACKUP_ZERO);
				}

				_shadowCopyManager.ReleaseShadowCopies(shadowCopyList);
				if (shadowCopyList.Count > 0)
				{
					_status.VSS = false;
					_taskManager.InsertLog("VSS Released");
				}

				_taskManager.Clear();

				if (_status.Quit || _status.Error)
				{
					_log.Debug(INSTANT_BACKUP_STOP);
					_taskManager.InsertLog($"{INSTANT_BACKUP_STOP} (Error: {_status.Error})");
				}
				else
				{
					_log.Debug(INSTANT_BACKUP_FINISH);
					_taskManager.InsertLog($"{INSTANT_BACKUP_FINISH} (Total: {_instant.Total}, Uploaded: {_instant.Upload})");
				}
				_status.Quit = true;
			}
			else if (_instant.Total != 0)
			{
				_taskManager.InsertLog("Resuming backup process...");
				InstantRunOnce();
			}
		}

		/// <summary>
		/// 인스턴트 백업 한 번 실행
		/// </summary>
		private void InstantRunOnce(List<ShadowCopy> shadowCopyList = null)
		{
			long lastPercent = -1;  // 마지막으로 기록한 퍼센트
			ExecuteBackup(shadowCopyList, item =>
			{
				_instant.Upload++;

				// 진행률 계산 (소수점 2자리까지)
				double percent = Math.Round((_instant.Upload / (double)_instant.Total) * 100, 2);

				// 1% 단위로 로그 기록
				long currentPercent = (long)Math.Floor(percent);
				if (lastPercent != currentPercent)
				{
					lastPercent = currentPercent;
					_taskManager.InsertLog($"Instant Backup Progress: {percent:F2}% ({_instant.Upload}/{_instant.Total} files)");
				}

				// 기존 진행률 업데이트 (100% 단위)
				double quotient = Math.Truncate(_instant.Upload / (double)_instant.Total);
				if (_instant.Percent < quotient)
				{
					_instant.Percent = (long)quotient;
				}
			});
			_log.Debug("InstantBackup End");
			_taskManager.InsertLog($"Backup batch completed: {_instant.Upload}/{_instant.Total} files processed");
		}
	}
}