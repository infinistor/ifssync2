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
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Linq;
using log4net;
using System.Reflection;

namespace IfsSync2Filter
{
	/// <summary>
	/// 파일 쓰기 작업 관리 클래스
	/// </summary>
	public class WriteFileManager
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly ConcurrentDictionary<string, FileWriteInfo> _processingFiles = new();
		private readonly Action<string> _createBackup;
		private bool _isRunning;
		private const int MIN_WAIT_SECONDS = 1;
		private const int MAX_WAIT_SECONDS = 10;
		private const long SIZE_THRESHOLD = 100 * 1024 * 1024; // 100MB

		public WriteFileManager(Action<string> createBackup)
		{
			_createBackup = createBackup;
			_isRunning = true;
			StartMonitoring();
		}

		public void AddOrUpdateFile(string filePath)
		{
			try
			{
				var currentTicks = Environment.TickCount64;
				var info = _processingFiles.GetOrAdd(filePath, _ => new FileWriteInfo
				{
					LastCheckTicks = currentTicks
				});

				// 이벤트가 발생할 때마다 시간 갱신
				info.LastCheckTicks = currentTicks;
				log.Debug($"File event occurred, reset wait time: {filePath}");
			}
			catch (Exception ex)
			{
				log.Error($"Error for file {filePath}: {ex.Message}");
			}
		}

		private void StartMonitoring()
		{
			Thread monitorThread = new(() =>
			{
				while (_isRunning)
				{
					try
					{
						CheckFiles();
					}
					catch (Exception ex)
					{
						log.Error($"Error in monitoring thread: {ex.Message}", ex);
					}
					Thread.Sleep(1000);
				}
			})
			{
				IsBackground = true
			};
			monitorThread.Start();
		}

		static int GetWaitTimeSeconds(long fileSize)
		{
			if (fileSize <= SIZE_THRESHOLD)
				return MIN_WAIT_SECONDS;

			// 100MB 이상인 경우 파일 크기에 비례하여 대기 시간 증가 (최대 10초)
			int waitTime = (int)(fileSize / SIZE_THRESHOLD * MIN_WAIT_SECONDS);
			return Math.Min(waitTime, MAX_WAIT_SECONDS);
		}

		private void CheckFiles()
		{
			foreach (var kvp in _processingFiles.ToList())
			{
				var filePath = kvp.Key;
				var info = kvp.Value;

				try
				{
					var fileInfo = new FileInfo(filePath);
					var currentTicks = Environment.TickCount64;

					if (!fileInfo.Exists)
					{
						_processingFiles.TryRemove(filePath, out _);
						log.Debug($"File removed from monitoring (not exists): {filePath}");
						continue;
					}

					int waitTime = GetWaitTimeSeconds(fileInfo.Length);
					var elapsedSeconds = (currentTicks - info.LastCheckTicks) / 1000;

					// 대기 시간이 지난 경우 백업 수행
					if (elapsedSeconds > waitTime)
					{
						_createBackup(filePath);
						_processingFiles.TryRemove(filePath, out _);
						log.Debug($"File backed up and removed from monitoring after wait time: {filePath}");
					}
				}
				catch (IOException ex)
				{
					log.Debug($"File {filePath} is currently in use: {ex.Message}");
				}
				catch (Exception ex)
				{
					log.Error($"Error checking file {filePath}: {ex.Message}", ex);
				}
			}
		}

		public void Stop()
		{
			_isRunning = false;
		}
	}
}