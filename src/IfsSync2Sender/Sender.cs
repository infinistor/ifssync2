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
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using IfsSync2Data;
using System.Collections.Generic;
using System.Diagnostics;
using Amazon.S3;
using System.Net;
using System.Linq;
using Amazon.S3.Model;

namespace IfsSync2Sender
{
	public class Sender
	{
		protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		const string CONNECT_FAILURE = "ConnectFailure";
		const string DEFAULT_ERROR_MESSAGE = "The network connection has been lost.";

		internal readonly TaskDbManager _taskManager;
		protected readonly JobDbManager _jobManager;
		protected readonly JobStatus _status;
		protected readonly string _bucketName;

		protected readonly ShadowCopyManager _shadowCopyManager;

		public JobData Job { get; protected set; }

		public int Delay { get; protected set; }
		public bool Pause { get; protected set; }

		protected Thread SenderThread;
		public bool IsAlive => SenderThread != null && SenderThread.IsAlive;
		public bool Quit => _status.Quit;

		protected UserData _user;
		protected S3Client _client;
		protected int _fetchCount;
		protected int _threadCount;
		protected long _multipartUploadFileSize;
		protected long _partSize;

		public Sender(JobData jobData, UserData userData, int fetchCount, int delayTime, int threadCount, long multipartUploadFileSize, long multipartUploadPartSize)
		{
			Pause = false;
			Job = jobData;
			_bucketName = userData.UserName.ToLower().Replace("_", "-");
			_user = userData;
			_client = new S3Client(_user);
			Delay = delayTime;
			_fetchCount = fetchCount;
			_threadCount = threadCount;
			_multipartUploadFileSize = multipartUploadFileSize;
			_partSize = multipartUploadPartSize;
			_taskManager = new TaskDbManager(jobData.JobName);
			_jobManager = new JobDbManager();
			_status = new JobStatus(Job.HostName, Job.JobName, true);

			_shadowCopyManager = new ShadowCopyManager();
		}

		#region Setup
		public void Run()
		{
			SenderThread = new Thread(Start);
			SenderThread.Start();
		}
		protected void End()
		{
			Pause = true;
			_status.Clear();
		}

		public void UpdateUser(UserData userData)
		{
			// 기존 작업 중지
			Pause = true;

			// 사용자 정보 업데이트
			_user = userData;
			_client = new S3Client(_user);

			// 작업 재개
			Pause = false;
		}
		public void UpdateJob(JobData jobData)
		{
			// 기존 작업 중지
			Pause = true;

			// 작업 정보 업데이트
			Job.CopyTo(jobData);

			// 작업 재개
			Pause = false;
		}
		public void Update(int fetchCount, int delayTime, int threadCount, long multipartUploadFileSize, long multipartUploadPartSize)
		{
			_fetchCount = fetchCount;
			Delay = delayTime;
			_threadCount = threadCount;
			_multipartUploadFileSize = multipartUploadFileSize;
			_partSize = multipartUploadPartSize;
		}

		public void Close()
		{
			End();
			log.Debug("Close");
		}
		#endregion

		public virtual void Start()
		{
			log.Debug("Start");

			if (!Login())
			{
				End();
				return;
			}

			switch (Job.Policy)
			{
				case JobData.PolicyType.Now:
					{
						log.Debug("Now policy is not supported in base Sender class");
						break;
					}
				case JobData.PolicyType.Schedule:
					{
						if (Job.IsInit)
						{
							Analysis();
							_jobManager.UpdateIsInit(Job, false);
						}
						while (!_status.Quit)
						{
							if (!Pause && Job.CheckToSchedules()) RunOnce();
							Thread.Sleep(Delay);
						}
						break;
					}
				case JobData.PolicyType.RealTime:
					{
						if (Job.IsInit)
						{
							Analysis();
							_jobManager.UpdateIsInit(Job, false);
						}
						while (!_status.Quit)
						{
							if (!Pause) RealTimeRunOnce();
							Thread.Sleep(Delay);
						}
						break;
					}
			}
			End();
		}

		#region Login
		protected bool Login()
		{
			const string LOGIN_FAIL_MESSAGE = "로그인실패! 네트워크 연결이 끊어졌거나 관리자의 접속허가가 필요할 수 있습니다.";
			try
			{
				if (CheckConnect(_client))
				{
					log.Info($"Login Success : {Job.JobName}");
					if (_status.Error) _taskManager.InsertLog("S3 Login Success!");
					_status.Error = false;
					return true;
				}
				else
				{
					log.Fatal($"Login fail : {_user.URL}");
					if (!_status.Error) _taskManager.InsertLog(LOGIN_FAIL_MESSAGE);
					_status.Error = true;
					End();
					return false;
				}
			}
			catch (AmazonS3Exception e)
			{
				log.Fatal($"Login fail.", e);
				if (!_status.Error) _taskManager.InsertLog(LOGIN_FAIL_MESSAGE);
				_status.Error = true;
				return false;
			}
			catch (Exception e)
			{
				string Message;
				if (e.Message.Contains(CONNECT_FAILURE)) Message = DEFAULT_ERROR_MESSAGE;
				else Message = "S3 Login Fail!";

				log.Fatal(Message, e);
				if (!_status.Error) _taskManager.InsertLog(Message);

				_status.Error = true;
				return false;
			}
		}
		#endregion

		#region Amazon S3
		static bool CheckConnect(S3Client client)
		{
			try
			{
				client.ListBuckets();
				return true;
			}
			catch (AggregateException e)
			{
				log.Error($"CheckConnect Failed. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}");
				return false;
			}
			catch (AmazonS3Exception e)
			{
				log.Error($"CheckConnect Failed. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}");
				return false;
			}
			catch (Exception e) { log.Error(e); return false; }
		}

		static bool ExistBucket(S3Client Client, string bucketName)
		{
			try
			{
				var isBucket = Client.DoesS3BucketExist(bucketName);
				log.Debug($"BucketName : {bucketName}, Result : {isBucket}");
				return isBucket;
			}
			catch (AggregateException e)
			{
				log.Error($"ExistBucket({bucketName}) Failed. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}");
				return false;
			}
			catch (AmazonS3Exception e)
			{
				log.Error($"ExistBucket({bucketName}) Failed. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}");
				return false;
			}
			catch (Exception e) { log.Error(e); return false; }
		}

		static bool ExistObject(S3Client Client, string BucketName, string ObjectName)
		{
			try
			{
				Client.GetObjectMetadata(BucketName, ObjectName);
				log.Debug($"ExistObject({BucketName}, {ObjectName}) : True");
				return true;
			}
			catch (AggregateException e)
			{
				log.Error($"ExistObject({BucketName}, {ObjectName}) Failed. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}");
				return false;
			}

		}

		static bool PutBucket(S3Client Client, string bucketName)
		{
			try
			{
				if (Client.DoesS3BucketExist(bucketName)) return false;
				var Response = Client.PutBucket(bucketName);

				if (Response.HttpStatusCode == System.Net.HttpStatusCode.OK)
				{
					log.Info($"PutBucket({bucketName}) : Create!!");
					return true;
				}
				else log.Error($"PutBucket({bucketName}) : Create failed");
			}
			catch (AggregateException e)
			{
				log.Error($"PutBucket({bucketName}) Failed. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}");
				return false;
			}
			catch (AmazonS3Exception e)
			{
				log.Error($"PutBucket({bucketName}) Failed. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}");
				return false;
			}
			catch (Exception e) { log.Error(e); }
			return false;
		}

		static S3Metadata HeadObject(S3Client client, string BucketName, string FilePath)
		{
			try
			{
				string ObjectName = FileNameChangeToS3ObjectName(FilePath);

				var response = client.GetObjectMetadata(BucketName, ObjectName); //Get Bucket request

				string MD5Hash = response.ETag.Replace("\"", string.Empty);

				log.Debug($"HeadObject({BucketName}) {ObjectName} / {MD5Hash}");
				return new S3Metadata(ObjectName, response);
			}
			catch (AggregateException e) { log.Error($"HeadObject failed. S3://{BucketName}/{FilePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}"); return null; }
			catch (AmazonS3Exception e) { log.Error($"HeadObject failed. S3://{BucketName}/{FilePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}"); return null; }
			catch (Exception e) { log.Error(e); return null; }
		}

		static string FileNameChangeToS3ObjectName(string DirectoryName) => DirectoryName.Replace("\\", "/").Replace(":", "");
		public static HttpStatusCode GetStatus(AggregateException e) => (e.InnerException is AmazonS3Exception e2) ? e2.StatusCode : HttpStatusCode.OK;
		public static string GetErrorCode(AggregateException e) => (e.InnerException is AmazonS3Exception e2) ? e2.ErrorCode : null;

		bool Upload(string filePath, string snapshotPath, out string errorMsg)
		{
			errorMsg = string.Empty;
			try
			{
				if (string.IsNullOrEmpty(snapshotPath)) snapshotPath = filePath;
				if (!File.Exists(snapshotPath))
				{
					errorMsg = $"File is not exists : {snapshotPath}";
					log.Error(errorMsg);
					return false;
				}

				string ObjectName = FileNameChangeToS3ObjectName(filePath);
				_client.Upload(_bucketName, ObjectName, snapshotPath, threadCount: _threadCount, minSizeBeforePartUpload: _multipartUploadFileSize, partSize: _partSize);
				log.Debug($"BucketName : {_bucketName} ObjectName : {ObjectName}");
				return true;
			}
			catch (AggregateException e)
			{
				errorMsg = $"DeleteObject Failed. S3://{_bucketName}/{filePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(errorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				errorMsg = $"DeleteObject Failed. S3://{_bucketName}/{filePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				errorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					log.Error(DEFAULT_ERROR_MESSAGE);
					if (!_status.Error) _taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					_status.Error = true;
				}
				return false;
			}
		}

		bool RenameObject(string filePath, string newFilePath, out string errorMsg)
		{
			try
			{
				string ObjectName = FileNameChangeToS3ObjectName(filePath);
				string NewObjectName = FileNameChangeToS3ObjectName(newFilePath);

				// 원본 파일이 존재하는지 확인
				if (!ExistObject(_client, _bucketName, ObjectName))
				{
					//원본 파일이 존재 하지 않는다면 원본 파일을 변경할 파일명으로 업로드
					log.Debug($"RenameObject({filePath}, {newFilePath}) : {ObjectName} is not exists. change to {newFilePath}");
					return Upload(newFilePath, "", out errorMsg);
				}
				// 원본 파일이 존재하는 경우 복사 후 삭제
				else
				{
					_client.CopyObject(_bucketName, ObjectName, _bucketName, NewObjectName);
					_client.DeleteObject(_bucketName, ObjectName);
					log.Debug($"BucketName : {_bucketName} ObjectName : {ObjectName} : {NewObjectName}");
				}
				errorMsg = string.Empty;
				return true;
			}
			catch (AggregateException e)
			{
				errorMsg = $"DeleteObject Failed. path : {filePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(errorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				errorMsg = $"DeleteObject Failed. path : {filePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				errorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					log.Error(DEFAULT_ERROR_MESSAGE);
					if (!_status.Error) _taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					_status.Error = true;
				}
				return false;
			}
		}

		bool DeleteObjects(string folderPath, out string errorMsg)
		{
			try
			{
				// s3 모든 파일 삭제
				var nextMarker = string.Empty;
				while (true)
				{
					var response = _client.ListObjects(_bucketName, prefix: folderPath, marker: nextMarker);
					var deleteList = response.S3Objects.Select(x => new KeyVersion() { Key = x.Key }).ToList();
					_client.DeleteObjects(new DeleteObjectsRequest() { BucketName = _bucketName, Objects = deleteList });

					if (response.IsTruncated) nextMarker = response.NextMarker;
					else break;
				}

				errorMsg = string.Empty;
				return true;
			}
			catch (AggregateException e)
			{
				errorMsg = $"DeleteObjects Failed. path : {folderPath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(errorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				errorMsg = $"DeleteObjects Failed. path : {folderPath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				errorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					log.Error(DEFAULT_ERROR_MESSAGE);
					if (!_status.Error) _taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					_status.Error = true;
				}
				return false;
			}
		}
		bool DeleteObject(string filePath, out string errorMsg)
		{
			try
			{
				string ObjectName = FileNameChangeToS3ObjectName(filePath);
				_client.DeleteObject(_bucketName, ObjectName);
				log.Debug($"BucketName : {_bucketName} ObjectName : {ObjectName}");
				errorMsg = string.Empty;
				return true;
			}
			catch (AggregateException e)
			{
				errorMsg = $"DeleteObject Failed. path : {filePath}. StatusCode : {GetStatus(e)}, ErrorCode : {GetErrorCode(e)}";
				log.Error(errorMsg);
				return false;
			}
			catch (AmazonS3Exception e)
			{
				errorMsg = $"DeleteObject Failed. path : {filePath}. StatusCode : {e.StatusCode}, ErrorCode : {e.ErrorCode}";
				log.Error(e);
				return false;
			}
			catch (Exception e)
			{
				errorMsg = e.Message;
				log.Error(e);
				if (e.Message.Contains(CONNECT_FAILURE))
				{
					log.Error(DEFAULT_ERROR_MESSAGE);
					if (!_status.Error) _taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					_status.Error = true;
				}
				return false;
			}
		}
		#endregion

		#region ShadowCopy
		#endregion

		#region Analysis
		protected int Analysis()
		{
			log.Info("Start");
			var sw = new Stopwatch();
			sw.Start();
			if (Job.Policy == JobData.PolicyType.Now) _status.Filter = true;
			_taskManager.Clear();

			try
			{
				//Analysis
				var extensionList = new List<string>(Job.WhiteFileExt);
				var directoryList = new List<string>(Job.Path);

				if (directoryList?.Count == 0)
				{
					log.Error("디렉토리 목록이 비어있습니다.");
					return 0;
				}

				int FileCount = 0;
				//Directory Search
				foreach (var directory in directoryList)
				{
					if (_status.Quit) break;
					if (string.IsNullOrWhiteSpace(directory))
					{
						log.Warn("빈 디렉토리 경로가 감지되었습니다.");
						continue;
					}

					try
					{
						FileCount += SubDirectory(directory, extensionList);
					}
					catch (UnauthorizedAccessException ex)
					{
						log.Error($"디렉토리 접근 권한이 없습니다: {directory}", ex);
					}
					catch (DirectoryNotFoundException ex)
					{
						log.Error($"디렉토리를 찾을 수 없습니다: {directory}", ex);
					}
					catch (Exception ex)
					{
						log.Error($"디렉토리 처리 중 오류 발생: {directory}", ex);
					}
				}

				if (Job.Policy == JobData.PolicyType.Now) _status.Filter = false;
				sw.Stop();
				log.Debug($"End. Time : {sw.ElapsedMilliseconds}ms / FileCount:{FileCount}");
				return FileCount;
			}
			catch (Exception ex)
			{
				log.Error("Analysis 처리 중 예기치 않은 오류가 발생했습니다.", ex);
				return 0;
			}
		}

		int SubDirectory(string ParentDirectory, List<string> ExtensionList)
		{
			if (string.IsNullOrWhiteSpace(ParentDirectory))
			{
				log.Error("상위 디렉토리 경로가 null 또는 비어있습니다.");
				return 0;
			}

			try
			{
				DirectoryInfo dInfoParent = new(ParentDirectory);

				if (!dInfoParent.Exists)
				{
					log.Warn($"디렉토리가 존재하지 않습니다: {ParentDirectory}");
					return 0;
				}

				int FileCount = 0;
				//add Folder List
				try
				{
					foreach (DirectoryInfo dInfo in dInfoParent.GetDirectories())
					{
						if (_status.Quit) break;
						if ((dInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
						{
							log.Debug($"숨김 디렉토리 건너뜀: {dInfo.FullName}");
							continue;
						}
						FileCount += SubDirectory(dInfo.FullName, ExtensionList);
					}
				}
				catch (UnauthorizedAccessException ex)
				{
					log.Error($"하위 디렉토리 접근 권한이 없습니다: {ParentDirectory}", ex);
				}
				catch (Exception ex)
				{
					log.Error($"하위 디렉토리 처리 중 오류 발생: {ParentDirectory}", ex);
				}

				FileCount += AddBackupFile(ParentDirectory, ExtensionList);

				return FileCount;
			}
			catch (Exception ex)
			{
				log.Error($"디렉토리 처리 중 예기치 않은 오류 발생: {ParentDirectory}", ex);
				return 0;
			}
		}

		int AddBackupFile(string ParentDirectory, List<string> ExtensionList)
		{
			if (string.IsNullOrWhiteSpace(ParentDirectory))
			{
				log.Error("디렉토리 경로가 null 또는 비어있습니다.");
				return 0;
			}

			if (ExtensionList == null)
			{
				log.Error("확장자 리스트가 null입니다.");
				return 0;
			}

			try
			{
				//add File List
				string[] files;
				try
				{
					files = Directory.GetFiles(ParentDirectory);
				}
				catch (UnauthorizedAccessException ex)
				{
					log.Error($"파일 목록 접근 권한이 없습니다: {ParentDirectory}", ex);
					return 0;
				}
				catch (DirectoryNotFoundException ex)
				{
					log.Error($"디렉토리를 찾을 수 없습니다: {ParentDirectory}", ex);
					return 0;
				}
				catch (Exception ex)
				{
					log.Error($"파일 목록 조회 중 오류 발생: {ParentDirectory}", ex);
					return 0;
				}

				TaskData.TaskTypeList taskName = TaskData.TaskTypeList.Upload;
				bool isAllExtensions = ExtensionList.Contains("ALL");
				int FileCount = 0;

				foreach (string file in files)
				{
					if (_status.Quit) break;

					try
					{
						var info = new FileInfo(file);
						if (!info.Exists)
						{
							log.Warn($"파일이 존재하지 않습니다: {file}");
							continue;
						}

						if ((info.Attributes & FileAttributes.System) == FileAttributes.System)
						{
							log.Debug($"시스템 파일 건너뜀: {file}");
							continue;
						}

						if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
						{
							log.Debug($"숨김 파일 건너뜀: {file}");
							continue;
						}

						// ALL 옵션이거나 확장자가 리스트에 포함된 경우 처리
						if ((isAllExtensions || ExtensionList.Contains(info.Extension.Replace(".", "").ToLower()))
								&& info.FullName.IndexOf('$') < 0)
						{
							try
							{
								var item = new TaskData(taskName, info.FullName, MainData.GetCurrentTime(), info.Length);
								_taskManager.Insert(item);
								FileCount++;
							}
							catch (Exception ex)
							{
								log.Error($"작업 데이터 추가 중 오류 발생: {file}", ex);
							}
						}
					}
					catch (IOException ex)
					{
						log.Error($"파일 정보 읽기 실패: {file}", ex);
					}
					catch (UnauthorizedAccessException ex)
					{
						log.Error($"파일 접근 권한이 없습니다: {file}", ex);
					}
					catch (Exception ex)
					{
						log.Error($"파일 처리 중 오류 발생: {file}", ex);
					}
				}
				return FileCount;
			}
			catch (Exception ex)
			{
				log.Error($"파일 처리 중 예기치 않은 오류 발생: {ParentDirectory}", ex);
				return 0;
			}
		}
		#endregion Analysis

		/// <summary>
		/// 백업 실행 공통 로직
		/// </summary>
		/// <param name="shadowCopyList">스냅샷 목록</param>
		/// <param name="processTask">작업 처리 후 실행할 콜백</param>
		protected void ExecuteBackup(List<ShadowCopy> shadowCopyList, Action<TaskData> processTask = null)
		{
			_status.Sender = true;

			while (!_status.Quit)
			{
				_taskManager.GetList(_fetchCount);
				if (_taskManager.TaskCount == 0) break;
				_status.RenameUpdate(_taskManager.TaskCount, _taskManager.TaskSize);

				// VSS 필요 여부 확인 후 스냅샷 생성
				bool needVSS = _shadowCopyManager.CheckNeedVSS(_taskManager.TaskList, Job.VSSFileExt);
				if (needVSS)
				{
					shadowCopyList ??= _shadowCopyManager.CreateShadowCopies(Job.Path);
					if (shadowCopyList.Count > 0) _status.VSS = true;
				}

				foreach (TaskData item in _taskManager.TaskList)
				{
					if (_status.Quit || Pause || !Job.CheckToSchedules()) { break; }

					// 스냅샷 경로 적용
					if (_status.VSS)
					{
						_shadowCopyManager.ApplySnapshotPathToTask(item, shadowCopyList);
					}

					if (!Backup(item))
					{
						End();
						break;
					}

					// 작업 처리 후 콜백 실행
					processTask?.Invoke(item);
				}

				if (_status.Quit || Pause) break;
				else Thread.Sleep(Delay);
			}

			// 스냅샷 해제
			_shadowCopyManager.ReleaseShadowCopies(shadowCopyList);
			if (shadowCopyList != null && shadowCopyList.Count > 0) _status.VSS = false;
			_status.Sender = false;
		}

		void RunOnce(List<ShadowCopy> shadowCopyList = null)
		{
			ExecuteBackup(shadowCopyList);
		}

		public void RealTimeRunOnce()
		{
			ExecuteBackup(null);
		}

		protected bool Backup(TaskData task)
		{
			if (!ExistBucket(_client, _bucketName))
			{
				log.Info($"Not exists Bucket : {_bucketName}");
				if (PutBucket(_client, _bucketName)) log.Info($"Create Bucket : {_bucketName}");
				else
				{
					log.Error($"Create Fail Bucket {_bucketName}");
					if (!_status.Error)
					{
						log.Error($"{DEFAULT_ERROR_MESSAGE} {_bucketName}");
						_taskManager.InsertLog(DEFAULT_ERROR_MESSAGE);
					}
					_status.Error = true;
					return false;
				}
			}

			switch (task.TaskType)
			{
				case TaskData.TaskTypeList.Upload:
					{
						if (string.IsNullOrWhiteSpace(task.SnapshotPath)) task.SnapshotPath = task.FilePath;

						//File Check
						var meta = HeadObject(_client, _bucketName, task.FilePath);

						// 중복 파일 처리
						if (meta != null)
						{
							// TODO: checksum 이 있을 경우 비교
							// if (meta.ChecksumAlgorithm != S3ChecksumAlgorithm.None)
							// {
							// 	var checksum = ChecksumCalculator.CalculateChecksum(task.FilePath, meta.ChecksumAlgorithm);
							// 	if (checksum.Equals(meta.Checksum, StringComparison.OrdinalIgnoreCase))
							// 	{
							// 		_taskManager.Delete(task);
							// 		log.Debug($"Duplicate file : {task.FilePath}");
							// 		return true;
							// 	}
							// }
							// // checksum 이 없을 경우 MD5Sum에 -가 없을 경우 비교
							// else
							// 멀티파트 업로드가 아닐 경우
							if (!string.IsNullOrWhiteSpace(meta.MD5) && !meta.MD5.Contains('-'))
							{
								var md5sum = MainData.CalculateMD5(task.FilePath);
								if (meta.MD5.Equals(md5sum, StringComparison.OrdinalIgnoreCase))
								{
									_taskManager.Delete(task);
									log.Debug($"Duplicate file : {task.FilePath}");
									return true;
								}
								else
								{
									log.Debug($"{task.FilePath} mismatch : {meta.MD5} != {md5sum}");
								}
							}
						}

						if (Upload(task.FilePath, task.SnapshotPath, out string ErrorMsg)) task.UploadFlag = true;
						else
						{
							if (_status.Error) return false;
							task.UploadFlag = false;
							task.Result = ErrorMsg;
						}
						task.UploadTime = MainData.GetCurrentTime();
						_taskManager.Update(task);
						break;
					}
				case TaskData.TaskTypeList.Rename:
					{
						if (RenameObject(task.FilePath, task.NewFilePath, out string ErrorMsg)) task.UploadFlag = true;
						else
						{
							if (_status.Error) return false;
							task.UploadFlag = false;
							task.Result = ErrorMsg;
						}
						task.UploadTime = MainData.GetCurrentTime();
						_taskManager.Update(task);
						break;
					}
				case TaskData.TaskTypeList.Delete:
					{
						// 폴더일 경우 폴더 내 모든 파일 삭제
						if (task.FilePath.EndsWith('\\'))
						{
							if (DeleteObjects(task.FilePath, out string ErrorMsg)) task.UploadFlag = true;
							else
							{
								if (_status.Error) return false;
								task.UploadFlag = false;
								task.Result = ErrorMsg;
							}
						}
						else if (DeleteObject(task.FilePath, out string ErrorMsg)) task.UploadFlag = true;
						else
						{
							if (_status.Error) return false;
							task.UploadFlag = false;
							task.Result = ErrorMsg;
						}

						task.UploadTime = MainData.GetCurrentTime();
						_taskManager.Update(task);
						break;
					}
			}

			if (task.UploadFlag) _status.UploadSuccess(task.FileSize);
			else _status.UploadFail();

			return true;
		}
	}
}

